using Bit.Core.Models.Response;
using System;

namespace Bit.Core.Exceptions
{
    public class ApiExceptionTlsAuthRequired : ApiException
    {
        public ApiExceptionTlsAuthRequired() { }

        public ApiExceptionTlsAuthRequired(ErrorResponse error)
            : this()
        {
            Error = error;
        }

        public ErrorResponse Error { get; set; }
    }
}
