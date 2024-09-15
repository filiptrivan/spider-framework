using Soft.Generator.Security.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class RegistrationVerificationResultDTO
    {
        public RegistrationVerificationResultStatusCodes Status { get; set; }
        public string Message { get; set; }
    }
}
