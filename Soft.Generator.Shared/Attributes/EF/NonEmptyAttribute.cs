using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    /// <summary>
    /// Use only on the Enumerable properties (for now only in combination with UIOrderedOneToManyAttrbiute)
    /// </summary>
    public class NonEmptyAttribute : Attribute
    {
        public NonEmptyAttribute() { }
    }
}
