using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    public class M2MExtendEntityAttribute : Attribute
    {
        public string WithManyProperty { get; set; }

        public M2MExtendEntityAttribute(string withManyProperty)
        {
            WithManyProperty = withManyProperty;
        }
    }
}
