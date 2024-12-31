using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.SourceGenerators.Models
{
    public class SoftClass
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

        public List<SoftProperty> Properties { get; set; } = new List<SoftProperty>();


        public List<SoftAttribute> Attributes { get; set; }

        public List<SoftMethod> Methods { get; set; }
    }
}
