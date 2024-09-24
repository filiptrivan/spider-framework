using Soft.Generator.Shared.Attributes;
using Soft.Generator.Shared.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    [CustomValidator("RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();")]
    [CustomValidator("RuleFor(x => x.Password).NotEmpty().Length(6, 20);")]
    //[CustomValidator("RuleFor(x => x.BrowserId).NotEmpty();")] // FT: I think there is no need for any validation, the code will not brake, and we are not saving the data in the database
    public partial class LoginDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string BrowserId { get; set; }
    }
}
