using Soft.Generator.DesktopApp.Attributes;
using Soft.Generator.DesktopApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    public class DomainFolderPath : ISoftEntity
    {
        [Identifier]
        public long Id { get; set; }

        public string Path { get; set; }

        public List<WebApplication> Applications { get; set; }
    }
}
