using System.Threading.Tasks;

namespace Bit.Core.Abstractions
{
    public interface ICertificateService
    {
        Task<bool> PickAndSaveCertificate();
        Task ResetSelectedCertificate();
        Task<bool> SetCertificateContainerFromStorageAsync();
    }
}
