using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soft.SourceGenerators.Models
{
    public class SoftMethod
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Body { get; set; }
        public IEnumerable<SyntaxNode> DescendantNodes { get; set; }
        public List<SoftAttribute> Attributes { get; set; }
    }
}
