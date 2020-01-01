using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Bit.Core.Models;

namespace Bit.Core.Abstractions
{
    public interface IHttpMessageHandler
    {
        void UseCertificateContainer(ICertificateContainer certificateContainer);
        HttpClientHandler AsClientHandler();
    }
}
