using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.DTO
{
    public class LoginVerificationTokenDTO
    {
        public string Email { get; set; }
        public long UserId { get; set; }
        public string BrowserId { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
