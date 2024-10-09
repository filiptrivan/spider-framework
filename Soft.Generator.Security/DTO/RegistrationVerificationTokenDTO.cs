using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class RegistrationVerificationTokenDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string BrowserId { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
