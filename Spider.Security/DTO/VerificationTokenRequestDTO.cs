using Spider.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.DTO
{
    [CustomValidator("RuleFor(x => x.VerificationCode).NotEmpty().Length(6);")]
    [CustomValidator("RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();")]
    public class VerificationTokenRequestDTO
    {
        public string VerificationCode { get; set; }
        public string BrowserId { get; set; }
        public string Email { get; set; }
    }
}
