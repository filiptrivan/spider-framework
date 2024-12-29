using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    public class ManyToOneRequiredAttribute : RequiredAttribute
    {
        public string WithManyProperty { get; set; }

        public ManyToOneRequiredAttribute(string withManyProperty) 
        {
            WithManyProperty = withManyProperty;
        }
    }
}
