using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    public class M2MMaintanceEntityKeyAttribute : Attribute
    {
        public string NavigationPropertyName { get; }

        public M2MMaintanceEntityKeyAttribute(string navigationPropertyName)
        {
            NavigationPropertyName = navigationPropertyName;
        }
    }
}
