using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Exceptions
{
    public class BusinessExceptionWithoutLog : ExceptionWithoutLog
    {
        public BusinessExceptionWithoutLog(string message) : base(message) { }
    }
}
