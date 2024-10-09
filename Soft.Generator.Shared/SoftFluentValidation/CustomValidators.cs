using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.SoftFluentValidation
{

    /// <summary>
    /// FT: If you are adding more custom validators, you need to change the generator
    /// Generator only support property validators, not the whole dto ones (eg. x => x, only x => x.Name)
    /// </summary>
    public static class CustomValidators
    {
        public static bool NotHaveWhiteSpace(string input)
        {
            return !input.Any(x => char.IsWhiteSpace(x));
        }
    }
}
