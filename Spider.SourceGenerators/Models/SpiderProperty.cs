﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.SourceGenerators.Models
{
    public class SpiderProperty
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string EntityName { get; set; } // TODO FT: Add to every case, you didn't finished this, but it works for now.

        public List<SpiderAttribute> Attributes { get; set; } = new();
    }
}
