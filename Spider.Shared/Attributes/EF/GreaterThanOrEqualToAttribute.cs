using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    /// <summary>
    /// Set this attribute to the numeric properties only.
    /// </summary>
    public class GreaterThanOrEqualToAttribute : Attribute
    {
        public GreaterThanOrEqualToAttribute(int number) { }
    }
}
