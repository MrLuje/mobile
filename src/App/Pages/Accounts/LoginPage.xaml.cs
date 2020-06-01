﻿using Bit.App.Models;
using Bit.Core;
using Bit.Core.Abstractions;
using Bit.Core.Models.Domain;
using Bit.Core.Utilities;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public partial class LoginPage : BaseContentPage
    {
        private readonly IMessagingService _messagingService;
        private readonly IBroadcasterService _broadcasterService;
        private readonly IStorageService _storageService;
        private readonly LoginPageViewModel _vm;
        private readonly AppOptions _appOptions;

        public LoginPage(string email = null, AppOptions appOptions = null)
        {
            _storageService = ServiceContainer.Resolve<IStorageService>("storageService");
            _messagingService = ServiceContainer.Resolve<IMessagingService>("messagingService");
            _messagingService.Send("showStatusBar", true);
            _appOptions = appOptions;
            _broadcasterService = ServiceContainer.Resolve<IBroadcasterService>("broadcasterService");
            InitializeComponent();
            _vm = BindingContext as LoginPageViewModel;
            _vm.Page = this;
            _vm.StartTwoFactorAction = () => Device.BeginInvokeOnMainThread(async () => await StartTwoFactorAsync());
            _vm.LoggedInAction = () => Device.BeginInvokeOnMainThread(async () => await LoggedInAsync());
            _vm.CloseAction = async () =>
            {
                _messagingService.Send("showStatusBar", false);
                await Navigation.PopModalAsync();
            };
            _vm.Email = email;
            MasterPasswordEntry = _masterPassword;
            if (Device.RuntimePlatform == Device.Android)
            {
                ToolbarItems.RemoveAt(0);
            }

            _email.ReturnType = ReturnType.Next;
            _email.ReturnCommand = new Command(() => _masterPassword.Focus());
        }

        public Entry MasterPasswordEntry { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _broadcasterService.Subscribe(nameof(LoginPage), async (message) =>
            {
                if(message.Command == "selectFileResult")
                {
                    FileSelected_InstallCertificate(message);
                }
                else if(message.Command == "installCertificateResult")
                {
                    if(await _vm.PickCertificate())
                        await _vm.LoadCertificateAndLogin();
                }
            });

            await _vm.InitAsync();
            if (string.IsNullOrWhiteSpace(_vm.Email))
            {
                RequestFocus(_email);
            }
            else
            {
                RequestFocus(_masterPassword);
            }
        }

        private void FileSelected_InstallCertificate(Message message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var data = message.Data as Tuple<byte[], string>;
                var fileData = data.Item1;
                var fileName = data.Item2;

                _vm.PromptInstallCertificate(fileData);
            });
        }

        private async void LogIn_Clicked(object sender, EventArgs e)
        {
            if (DoOnce())
            {
                await _vm.LogInAsync();
            }
        }

        private void Hint_Clicked(object sender, EventArgs e)
        {
            if (DoOnce())
            {
                Navigation.PushModalAsync(new NavigationPage(new HintPage()));
            }
        }

        private async void Close_Clicked(object sender, EventArgs e)
        {
            if (DoOnce())
            {
                _vm.CloseAction();
            }
        }

        private async Task StartTwoFactorAsync()
        {
            var page = new TwoFactorPage();
            await Navigation.PushModalAsync(new NavigationPage(page));
        }

        private async Task LoggedInAsync()
        {
            if (_appOptions != null)
            {
                if (_appOptions.FromAutofillFramework && _appOptions.SaveType.HasValue)
                {
                    Application.Current.MainPage = new NavigationPage(new AddEditPage(appOptions: _appOptions));
                    return;
                }
                if (_appOptions.Uri != null)
                {
                    Application.Current.MainPage = new NavigationPage(new AutofillCiphersPage(_appOptions));
                    return;
                }
            }
            var previousPage = await _storageService.GetAsync<PreviousPageInfo>(Constants.PreviousPageKey);
            if (previousPage != null)
            {
                await _storageService.RemoveAsync(Constants.PreviousPageKey);
            }
            Application.Current.MainPage = new TabsPage(_appOptions, previousPage);
        }
    }
}
