//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net.Http;
//using System.Security.Authentication;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Android;
//using Android.App;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Security;
//using Android.Support.V4.App;
//using Android.Views;
//using Android.Widget;
//using Bit.App.Abstractions;
//using Bit.Core.Abstractions;
//using Bit.Core.Models;
//using Bit.Core.Utilities;
//using Java.Security;
//using Java.Security.Cert;
//using Java.Util;
//using Javax.Net.Ssl;
//using Xamarin.Android.Net;

//namespace Bit.Droid
//{
//    public class HttpMessageHandler : HttpClientHandler, IHttpMessageHandler
//    {
//        private SSLContext sslContext;
//        private readonly ITrustManager[] trustManagers;
//        private IKeyManager[] keyManagers = null;


//        private static String getThumbprint(X509Certificate cert)
//        {
//            MessageDigest md = MessageDigest.GetInstance("SHA-1");
//            byte[] der = cert.GetEncoded();
//            md.Update(der);
//            byte[] digest = md.Digest();
//            String digestHex = BitConverter.ToString(digest).Replace("-", "");
//            return digestHex.ToLower();
//        }

//        public HttpMessageHandler()
//        {
//            //var store = KeyStore.GetInstance("AndroidKeyStore");
//            //var store = KeyStore.GetInstance("AndroidCAStore");
//            //store.Load(null, null);
//            //var dummy = store.GetEntry("bw_user", null);

//            //var aliases = store.Aliases();
//            //foreach (string alias in Collections.List(aliases))
//            //{
//            //    X509Certificate cert = (X509Certificate)store.GetCertificate(alias);
//            //    var issuer = cert.IssuerDN.Name;
//            //    var f = ";";


//            //    if (getThumbprint(cert) == "eeb71f71bd4638dee4025414b8b49cbb08f2237b")//de101c6418941c22bcc7488bb49d111ecea4d0e4
//            //    {

//            //        //var cert2 = new System.Security.Cryptography.X509Certificates.X509Certificate2(cert.GetEncoded());
//            //        //var tb = cert2.Thumbprint;
//            //        //this.ClientCertificates.Add(cert2);
//            //    }
//            //}


//            //trustManagers = GetTrustManagers();
//            //sslContext = GetSSLContext();
//            //LoadAllKeys();
//        }

//        public new SslProtocols SslProtocols { get; set; } = SslProtocols.Tls12;


//        private SSLContext GetSSLContext()
//        {
//            string protocol;
//            if (SslProtocols == SslProtocols.Tls11)
//            {
//                protocol = "TLSv1.1";
//            }
//            else if (SslProtocols == SslProtocols.Tls || SslProtocols == SslProtocols.Tls12)
//            {
//                protocol = "TLSv1.2";
//            }
//            else
//            {
//                throw new InvalidOperationException("unsupported ssl protocol: " + SslProtocols.ToString());
//            }
//            var ctx = SSLContext.GetInstance(protocol);
//            ctx.Init(keyManagers, trustManagers, null);
//            return ctx;
//        }

//        private ITrustManager[] GetTrustManagers()
//        {
//            TrustManagerFactory trustManagerFactory = TrustManagerFactory.GetInstance(TrustManagerFactory.DefaultAlgorithm);
//            var store = KeyStore.GetInstance("AndroidCAStore");
//            trustManagerFactory.Init((KeyStore)null);
//            return trustManagerFactory.GetTrustManagers();
//        }

//        public void LoadAllKeys()
//        {
//            var store = KeyStore.GetInstance("AndroidCAStore");
//            var kmf = KeyManagerFactory.GetInstance("x509");
//            kmf.Init(store, null);
//            keyManagers = kmf.GetKeyManagers();
//            SSLContext newContext = GetSSLContext();
//            sslContext = newContext;
//        }


//        public void SetClientCertificate(byte[] pkcs12, char[] password)
//        {
//            keyManagers = GetKeyManagersFromClientCert(pkcs12, password);
//            SSLContext newContext = GetSSLContext();
//            sslContext = newContext;
//        }

//        private IKeyManager[] GetKeyManagersFromClientCert(byte[] pkcs12, char[] password)
//        {
//            if (pkcs12 != null)
//            {
//                using (MemoryStream memoryStream = new MemoryStream(pkcs12))
//                {
//                    KeyStore keyStore = KeyStore.GetInstance("pkcs12");
//                    keyStore.Load(memoryStream, password);
//                    KeyManagerFactory kmf = KeyManagerFactory.GetInstance("x509");
//                    kmf.Init(keyStore, password);
//                    return kmf.GetKeyManagers();
//                }
//            }
//            return null;
//        }

//        protected override async  Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//        {
//            try
//            {
//                return await base.SendAsync(request, cancellationToken);
//            }
//            catch (HttpRequestException httpEx) when (httpEx.InnerException is AuthenticationException)
//            {
//                throw;
//                var path = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).Path, "user.pfx");
//                var cert2 = new System.Security.Cryptography.X509Certificates.X509Certificate2(path, "jahz5Hoos$$$jahz5Hoos$$$", System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet);
//                this.ClientCertificates.Add(cert2);

//                return await this.SendAsync(request, cancellationToken);
//            }
//        }

//        public void UseCertificateContainer(ICertificateContainer certificateContainer)
//        {
            
//        }

//        public void ClearCertificateContainer()
//        {
            
//        }

//        public HttpClientHandler AsClientHandler()
//        {
//            return this;
//        }

//        //protected override SSLSocketFactory ConfigureCustomSSLSocketFactory(HttpsURLConnection connection)
//        //{
//        //    //    this.ClientCertificateOptions = System.Net.Http.ClientCertificateOption.Manual;

//        //    SSLSocketFactory socketFactory = sslContext.SocketFactory;
//        //    //    if (connection != null)
//        //    //    {
//        //    //        connection.SSLSocketFactory = socketFactory;
//        //    //    }




//        //    return socketFactory;
//        //}
//    }
//}
