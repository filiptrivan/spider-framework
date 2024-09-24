using Soft.Generator.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    [CustomValidator("RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();")]
    [CustomValidator("RuleFor(x => x.NewPassword).NotEmpty().Length(6, 20);")]
    public class ForgotPasswordDTO
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string BrowserId { get; set; }

    }
}
