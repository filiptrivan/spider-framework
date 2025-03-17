using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Exceptions
{
    public class BusinessException : Exception
    {
        public int StatusCode { get; set; } = StatusCodes.Status400BadRequest;

        // Constructor
        public BusinessException() : base() { }

        // Constructor with message
        public BusinessException(string message) : base(message) { }
    }
}
