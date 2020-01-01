using Bit.Core.Models;
using Java.Security;

namespace Bit.Droid
{
    public class AndroidCertificateContainer : ICertificateContainer<Java.Security.Cert.X509Certificate, IPrivateKey>
    {
        public Java.Security.Cert.X509Certificate Certificate { get; set; }

        public string Alias { get; set; }

        public IPrivateKey PrivateKeyRef { get; internal set; }
        public bool IsEmpty => this.Certificate == null && string.IsNullOrWhiteSpace(this.Alias);

        public static ICertificateContainer Empty => new AndroidCertificateContainer();
    }
}
