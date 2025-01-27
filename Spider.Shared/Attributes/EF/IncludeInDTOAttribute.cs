using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    /// <summary>
    /// Set this attribute to the property you want generated in the DTO.
    /// It only makes sense for enumerable properties(because they are not generated in a DTO by default).
    /// The generated property in DTO will not be included in the mapping library.
    /// </summary>
    public class IncludeInDTOAttribute : Attribute
    {
    }
}
