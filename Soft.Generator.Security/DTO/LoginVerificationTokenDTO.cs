using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class LoginVerificationTokenDTO
    {
        public string Email { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
