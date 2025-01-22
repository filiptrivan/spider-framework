using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.DTO
{
    public class RegistrationVerificationTokenDTO
    {
        public string Email { get; set; }
        public string BrowserId { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
