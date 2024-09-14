using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class VerificationTokenRequestDTO
    {
        public string VerificationToken { get; set; }
        public string AccessToken { get; set; }
        public string BrowserId { get; set; }
    }
}
