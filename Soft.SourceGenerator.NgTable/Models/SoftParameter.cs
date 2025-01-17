using System;
using System.Collections.Generic;
using System.Text;

namespace Soft.SourceGenerators.Models
{
    public class SoftParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<SoftAttribute> Attributes { get; set; } = new();
    }
}
