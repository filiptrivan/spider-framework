using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    public class GreaterThanOrEqualToAttribute : Attribute
    {
        public GreaterThanOrEqualToAttribute(int number) { }
    }
}
