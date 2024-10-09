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
        public List<SoftProperty> Properties { get; set; } = new List<SoftProperty>();

        /// <summary>
        /// For the DTO classes
        /// </summary>
        public bool IsGenerated { get; set; }
    }
}
