using Soft.Generator.DesktopApp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    public class DomainFolderPath
    {
        [Identifier]
        public long Id { get; set; }

        public string Path { get; set; }

        public List<WebApplication> Applications { get; set; }
    }
}
