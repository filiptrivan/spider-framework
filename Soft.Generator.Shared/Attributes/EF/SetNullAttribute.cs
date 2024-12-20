using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    public class SetNullAttribute : Attribute
    {
        public string WithManyProperty { get; set; }

        public SetNullAttribute(string withManyProperty)
        {
            WithManyProperty = withManyProperty;
        }
    }
}
