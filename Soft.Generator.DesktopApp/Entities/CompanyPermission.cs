using Soft.Generator.DesktopApp.Attributes;
using Soft.Generator.DesktopApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    /// <summary>
    /// Cascade delete is done in sql
    /// </summary>
    [ManyToMany]
    public class CompanyPermission : ISoftEntity
    {
        public virtual Company Company { get; set; }

        public virtual Permission Permission { get; set; }
    }
}
