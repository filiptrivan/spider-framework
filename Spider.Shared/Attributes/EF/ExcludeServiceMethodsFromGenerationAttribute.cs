using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    /// <summary>
    /// All the logic that should be generated in the BusinessServiceGenerated class for this property will not be generated.
    /// </summary>
    public class ExcludeServiceMethodsFromGenerationAttribute : Attribute
    {
        public ExcludeServiceMethodsFromGenerationAttribute() { }
    }
}
