using Spider.Security.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.DTO
{
    public class RegistrationVerificationResultDTO
    {
        public RegistrationVerificationResultStatusCodes Status { get; set; }
        public string Message { get; set; }
    }
}
