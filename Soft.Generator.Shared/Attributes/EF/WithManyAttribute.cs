using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    public class WithManyAttribute : Attribute
    {
        public string WithMany { get; set; }

        public WithManyAttribute(string withMany) 
        {
            WithMany = withMany;    
        }
    }
}
