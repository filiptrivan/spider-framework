using Soft.Generator.DesktopApp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    [ManyToMany]
    public class CompanyPermission
    {
        public virtual Company Company { get; set; }

        public virtual Permission Permission { get; set; }
    }
}
