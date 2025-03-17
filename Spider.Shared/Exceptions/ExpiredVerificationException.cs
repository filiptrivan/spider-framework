using Microsoft.AspNetCore.Http;
using Spider.Shared.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Exceptions
{
    public class ExpiredVerificationException : Exception
    {
        public int StatusCode { get; set; } = StatusCodes.Status400BadRequest;

        // Constructor
        public ExpiredVerificationException() : base(SharedTerms.ExpiredVerificationCodeException) { }

        // Constructor with message
        public ExpiredVerificationException(string message) : base(message) { }
    }
}
