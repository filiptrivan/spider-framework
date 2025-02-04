using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.SourceGenerators.Models
{
    public class SpiderClass
    {
        public string Name { get; set; }
        public string Namespace { get; set; }

        /// <summary>
        /// Here is only one base type, no interfaces
        /// </summary>
        public string BaseType { get; set; }

        public bool IsAbstract { get; set; }

        public string ControllerName { get; set; }

        /// <summary>
        /// For the DTO classes
        /// </summary>
        public bool IsGenerated { get; set; }

        public List<SpiderProperty> Properties { get; set; } = new();


        public List<SpiderAttribute> Attributes { get; set; } = new();

        public List<SpiderMethod> Methods { get; set; } = new();
    }
}
