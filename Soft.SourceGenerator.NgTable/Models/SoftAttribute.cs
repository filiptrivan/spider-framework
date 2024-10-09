using System;
using System.Collections.Generic;
using System.Text;

namespace Soft.SourceGenerators.Models
{
    public class SoftAttribute
    {
        public string Name { get; set; }

        /// <summary>
        /// Doesn't handle if more values are in the prenteces, eg. [Attribute("First", "Second")]
        /// </summary>
        public string Value { get; set; }
    }
}
