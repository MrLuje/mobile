using System;
using System.Threading.Tasks;
using Bit.App.Abstractions;
using Bit.Core;
using Bit.Core.Abstractions;
using Bit.Core.Models;

namespace Bit.App.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly IDeviceActionService _deviceActionService;
        private readonly IStorageService _storageService;
        private readonly Func<IApiService> _apiServiceFunc;

        public CertificateService(IDeviceActionService deviceActionService, IStorageService mobileStorageService, Func<IApiService> apiService)
        {
            _deviceActionService = deviceActionService;
            _storageService = mobileStorageService;
            _apiServiceFunc = apiService;
        }

        private async Task<ICertificateContainer> SelectCertificateAsync(string alias)
        {
            var cert = await Task.Run(() => _deviceActionService.LoadCertificateFromAlias(alias));
            return cert;
        }

        public async Task<bool> PickCertificate()
        {
            var alias = await _deviceActionService.PickExistingCertificateForUserCredentials();
            if(string.IsNullOrWhiteSpace(alias))
                return false;

            await _storageService.SaveAsync(Constants.TlsAuthCertificateAliasKey, alias);
            return true;
        }

        public async Task<bool> PickAndSaveCertificate()
        {
            var alias = await _deviceActionService.PickExistingCertificateForUserCredentials();
            if(string.IsNullOrWhiteSpace(alias))
                return false;

            await _storageService.SaveAsync(Constants.TlsAuthCertificateAliasKey, alias);
            return true;
        }

        public async Task ResetSelectedCertificate()
        {
            await _storageService.RemoveAsync(Constants.TlsAuthCertificateAliasKey);
            _apiServiceFunc().SetCertificateContainer(null);
        }

        public async Task<bool> SetCertificateContainerFromStorageAsync()
        {
            var certAlias = await _storageService.GetAsync<string>(Constants.TlsAuthCertificateAliasKey);
            if(string.IsNullOrWhiteSpace(certAlias))
                return false;

            var certContainer = await Task.Run(() => _deviceActionService.LoadCertificateFromAlias(certAlias));
            if(certContainer.IsEmpty)
                return false;

            _apiServiceFunc().SetCertificateContainer(certContainer);
            return true;
        }
    }
}
