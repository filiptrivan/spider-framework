using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    public class ManyToOneRequiredAttribute : RequiredAttribute
    {
        public ManyToOneRequiredAttribute() 
        {
        }
    }
}
