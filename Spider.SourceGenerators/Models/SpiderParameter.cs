using System;
using System.Collections.Generic;
using System.Text;

namespace Spider.SourceGenerators.Models
{
    public class SpiderParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<SpiderAttribute> Attributes { get; set; } = new();
    }
}
