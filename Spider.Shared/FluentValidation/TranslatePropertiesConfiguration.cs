using Microsoft.Extensions.Options;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Spider.Shared.Terms;

namespace Spider.Shared.FluentValidation
{
    public class TranslatePropertiesConfiguration : IConfigureOptions<MvcOptions>
    {
        // TODO FT: if you will some day transfer this to frm, make interface and class for translators and pass it through DI
        public TranslatePropertiesConfiguration()
        {
        }
        public void Configure(MvcOptions options)
        {
         ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression) => {
             return PropertyTerms.ResourceManager.GetString(memberInfo.Name, System.Globalization.CultureInfo.CurrentCulture);
         };
        }
    }
}
