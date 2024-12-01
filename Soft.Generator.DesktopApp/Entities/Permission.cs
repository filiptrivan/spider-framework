using Soft.Generator.DesktopApp.Attributes;
using Soft.Generator.DesktopApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    public class Permission : ISoftEntity
    {
        [Identifier]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        [ManyToMany("CompanyPermission")]
        public virtual List<Company> Companies { get; set; }
    }
}
