﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spider.SourceGenerators.Models
{
    public class SpiderMethod
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Body { get; set; }
        public List<SpiderParameter> Parameters { get; set; }
        public IEnumerable<SyntaxNode> DescendantNodes { get; set; }
        public List<SpiderAttribute> Attributes { get; set; }
    }
}
