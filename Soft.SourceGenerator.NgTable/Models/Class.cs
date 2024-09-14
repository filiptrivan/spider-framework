using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.SourceGenerator.NgTable.Models
{
    public class Class
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; } = new List<Property>();
    }
}
