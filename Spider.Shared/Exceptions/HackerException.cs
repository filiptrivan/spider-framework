using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Exceptions
{
    public class HackerException : Exception
    {
        // Constructor
        public HackerException() : base("Someone is trying to harm our system.") { }

        // Constructor with message
        public HackerException(string message) : base(message) { }
    }
}
