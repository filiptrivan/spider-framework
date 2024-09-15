using Soft.Generator.Security.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class LoginVerificationResultDTO
    {
        public LoginVerificationResultStatusCodes Status { get; set; }
        public string Message { get; set; }
    }
}
