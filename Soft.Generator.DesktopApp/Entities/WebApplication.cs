using Soft.Generator.DesktopApp.Attributes;
using Soft.Generator.DesktopApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    public class WebApplication : ISoftEntity
    {
        [Identifier]
        public long Id { get; set; }

        public string Name { get; set; }

        [ManyToOneRequired]
        public virtual Company Company { get; set; }

        public virtual Setting Setting { get; set; }
    }
}
