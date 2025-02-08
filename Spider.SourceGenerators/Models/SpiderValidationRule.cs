using System;
using System.Collections.Generic;
using System.Text;

namespace Spider.SourceGenerators.Models
{
    public class SpiderValidationRule
    {
        public SpiderProperty Property { get; set; }
        public List<SpiderValidationRulePart> ValidationRuleParts { get; set; } = new();
    }
}
