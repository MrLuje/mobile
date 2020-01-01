using System;
using System.Collections.Generic;
using System.Text;

namespace Bit.Core.Models
{
    public interface ICertificateContainer<T, U> : ICertificateContainer
    {
        string Alias { get; }
        U PrivateKeyRef { get; }
        T Certificate { get; }
    }

    public interface ICertificateContainer
    {
        bool IsEmpty { get; }
    }
}
