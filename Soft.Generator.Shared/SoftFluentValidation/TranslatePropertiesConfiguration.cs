using Microsoft.Extensions.Options;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Soft.Generator.Shared.Terms;

namespace Soft.Generator.Shared.SoftFluentValidation
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
