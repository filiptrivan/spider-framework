using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Exceptions
{
    public class ExceptionWithoutLog : Exception
    {
        public ExceptionWithoutLog(string message) : base(message) { }
    }
}
