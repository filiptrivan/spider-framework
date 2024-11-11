using Soft.Generator.DesktopApp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    public class GeneratedFile 
    {
        [Identifier]
        public long Id { get; set; }

        public string DisplayName { get; set; }

        public string ClassName { get; set; }

        public string Namespace { get; set; }

        public bool Regenerate { get; set; }

        [ManyToOneRequired]
        public virtual WebApplication Application { get; set; }

        public virtual DomainFolderPath DomainFolderPath { get; set; }
    }
}
