using Spider.Shared.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Exceptions
{
    public class UnauthorizedException : Exception
    {
        // Constructor
        public UnauthorizedException() : base(SharedTerms.UnauthorizedAccessExceptionMessage) { }

        // Constructor with message
        public UnauthorizedException(string message) : base(message) { }
    }
}