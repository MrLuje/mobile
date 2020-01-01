using Bit.App.Abstractions;
using Bit.App.Resources;
using Bit.Core;
using Bit.Core.Abstractions;
using Bit.Core.Exceptions;
using Bit.Core.Models;
using Bit.Core.Utilities;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public class LoginPageViewModel : BaseViewModel
    {
        private const string Keys_RememberedEmail = "rememberedEmail";
        private const string Keys_RememberEmail = "rememberEmail";

        private readonly IDeviceActionService _deviceActionService;
        private readonly IAuthService _authService;
        private readonly ISyncService _syncService;
        private readonly IStorageService _storageService;
        private readonly IPlatformUtilsService _platformUtilsService;
        private readonly IStateService _stateService;
        private readonly ICertificateService _certificateService;

        private bool _showPassword;
        private string _email;
        private string _masterPassword;
        private bool _hideHintButton;

        public LoginPageViewModel()
        {
            _deviceActionService = ServiceContainer.Resolve<IDeviceActionService>("deviceActionService");
            _authService = ServiceContainer.Resolve<IAuthService>("authService");
            _syncService = ServiceContainer.Resolve<ISyncService>("syncService");
            _storageService = ServiceContainer.Resolve<IStorageService>("storageService");
            _platformUtilsService = ServiceContainer.Resolve<IPlatformUtilsService>("platformUtilsService");
            _stateService = ServiceContainer.Resolve<IStateService>("stateService");
            _certificateService = ServiceContainer.Resolve<ICertificateService>("certificateService");

            PageTitle = AppResources.Bitwarden;
            TogglePasswordCommand = new Command(TogglePassword);
            LogInCommand = new Command(async () => await LogInAsync());
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value,
                additionalPropertyNames: new string[]
                {
                    nameof(ShowPasswordIcon)
                });
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string MasterPassword
        {
            get => _masterPassword;
            set => SetProperty(ref _masterPassword, value);
        }

        public Command LogInCommand { get; }
        public Command TogglePasswordCommand { get; }
        public string ShowPasswordIcon => ShowPassword ? "" : "";
        public bool RememberEmail { get; set; }
        public Action StartTwoFactorAction { get; set; }
        public Action LoggedInAction { get; set; }
        public Action CloseAction { get; set; }

        public bool HideHintButton
        {
            get => _hideHintButton;
            set => SetProperty(ref _hideHintButton, value);
        }
        
        public async Task InitAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                Email = await _storageService.GetAsync<string>(Keys_RememberedEmail);
            }
            var rememberEmail = await _storageService.GetAsync<bool?>(Keys_RememberEmail);
            RememberEmail = rememberEmail.GetValueOrDefault(true);
        }

        public async Task LogInAsync()
        {
            if (Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.None)
            {
                await _platformUtilsService.ShowDialogAsync(AppResources.InternetConnectionRequiredMessage,
                    AppResources.InternetConnectionRequiredTitle);
                return;
            }
            if (string.IsNullOrWhiteSpace(Email))
            {
                await _platformUtilsService.ShowDialogAsync(
                    string.Format(AppResources.ValidationFieldRequired, AppResources.EmailAddress),
                    AppResources.AnErrorHasOccurred,
                    AppResources.Ok);
                return;
            }
            if (!Email.Contains("@"))
            {
                await _platformUtilsService.ShowDialogAsync(AppResources.InvalidEmail, AppResources.AnErrorHasOccurred,
                    AppResources.Ok);
                return;
            }
            if (string.IsNullOrWhiteSpace(MasterPassword))
            {
                await _platformUtilsService.ShowDialogAsync(
                    string.Format(AppResources.ValidationFieldRequired, AppResources.MasterPassword),
                    AppResources.AnErrorHasOccurred,
                    AppResources.Ok);
                return;
            }

            ShowPassword = false;
            try
            {
                await _deviceActionService.ShowLoadingAsync(AppResources.LoggingIn);
                var response = await _authService.LogInAsync(Email, MasterPassword);
                MasterPassword = string.Empty;
                if (RememberEmail)
                {
                    await _storageService.SaveAsync(Keys_RememberedEmail, Email);
                }
                else
                {
                    await _storageService.RemoveAsync(Keys_RememberedEmail);
                }
                await _deviceActionService.HideLoadingAsync();
                if (response.TwoFactor)
                {
                    StartTwoFactorAction?.Invoke();
                }
                else
                {
                    var disableFavicon = await _storageService.GetAsync<bool?>(Constants.DisableFaviconKey);
                    await _stateService.SaveAsync(Constants.DisableFaviconKey, disableFavicon.GetValueOrDefault());
                    var task = Task.Run(async () => await _syncService.FullSyncAsync(true));
                    LoggedInAction?.Invoke();
                }
            }
            catch(ApiExceptionTlsAuthRequired e)
            {
                if(!string.IsNullOrWhiteSpace(await GetCertificateAlias()))
                {
                    await this.LoadCertificateAndLogin();
                }
                else
                {
                    //TODO: proper resources
                    var res = await _deviceActionService.DisplayAlertAsync("Auth failed", "A certificate is required to connect to this server", "Choose an installed certificate", "Install and use a new certificate", "Cancel");
                    if(res == "Install and use a new certificate")
                    {
                        await _deviceActionService.HideLoadingAsync();
                        await _deviceActionService.SelectFileAsync();
                    }
                    else if(res == "Choose an installed certificate")
                    {
                        await _deviceActionService.HideLoadingAsync();
                        if(await this.PickCertificate())
                            await this.LoadCertificateAndLogin();

                    }
                    else if(res == "Cancel")
                    {
                        await _deviceActionService.HideLoadingAsync();
                    }
                    else if(e?.Error != null)
                    {
                        await _platformUtilsService.ShowDialogAsync(e.Error.GetSingleMessage(),
                            AppResources.AnErrorHasOccurred);
                    }
                }
            }
            catch (ApiException e)
            {
                await _deviceActionService.HideLoadingAsync();
                if (e?.Error != null)
                {
                    await _platformUtilsService.ShowDialogAsync(e.Error.GetSingleMessage(),
                        AppResources.AnErrorHasOccurred);
                }
            }
        }

        private async Task<string> GetCertificateAlias()
        {
            return await _storageService.GetAsync<string>(Constants.TlsAuthCertificateAliasKey);
        }

        public async Task LoadCertificateAndLogin()
        {
            var success = await _certificateService.SetCertificateContainerFromStorageAsync();
            if(success)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await this.LogInAsync();
                });
            }
        }

        public async Task<bool> PickCertificate()
        {
            try
            {
                return await _certificateService.PickAndSaveCertificate();
            }
            catch(System.Exception e)
            {
                await _platformUtilsService.ShowDialogAsync(e.Message, AppResources.AnErrorHasOccurred);
                return false;
            }
        }

        public void PromptInstallCertificate(byte[] fileData)
        {
            _deviceActionService.PromptInstallCertificate(fileData);
        }

        public void TogglePassword()
        {
            ShowPassword = !ShowPassword;
            (Page as LoginPage).MasterPasswordEntry.Focus();
        }
    }
}
