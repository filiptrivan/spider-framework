using Soft.Generator.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    //[CustomValidator("RuleFor(x => x.Provider).NotEmpty();")] // FT: For now we are not doint anything with this because we only have google
    //[CustomValidator("RuleFor(x => x.IdToken).NotEmpty();")] // FT: I think there is no need for any validation, the code will not brake, and we are not saving the data in the database
    //[CustomValidator("RuleFor(x => x.BrowserId).NotEmpty();")]
    public class ExternalProviderDTO
    {
        //public string Provider { get; set; }
        public string IdToken { get; set; }
        public string BrowserId { get; set; }
    }
}
