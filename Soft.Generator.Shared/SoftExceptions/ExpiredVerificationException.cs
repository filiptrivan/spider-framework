using Soft.Generator.Shared.Terms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.SoftExceptions
{
    public class ExpiredVerificationException : Exception
    {
        // Constructor
        public ExpiredVerificationException() : base(SharedTerms.ExpiredVerificationCodeException) { }

        // Constructor with message
        public ExpiredVerificationException(string message) : base(message) { }
    }
}
