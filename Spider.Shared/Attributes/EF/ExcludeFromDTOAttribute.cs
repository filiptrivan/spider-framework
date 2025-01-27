using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    /// <summary>
    /// Set this attribute to the property you don't want generated in the DTO.
    /// </summary>
    public class ExcludeFromDTOAttribute : Attribute
    {
    }
}
