using Soft.Generator.Shared.Terms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.SoftExceptions
{
    public class UnauthorizedException : Exception
    {
        // Constructor
        public UnauthorizedException() : base(SharedTerms.UnauthorizedAccessExceptionMessage) { }

        // Constructor with message
        public UnauthorizedException(string message) : base(message) { }
    }
}