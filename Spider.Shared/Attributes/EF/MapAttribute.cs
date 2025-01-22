using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    /// <summary>
    /// When you put this attribute on list property in the domain model: It's made only for manual mapping, it's not included in the mapping library.
    /// </summary>
    public class MapAttribute : Attribute
    {
    }
}
