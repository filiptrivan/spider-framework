using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    /// <summary>
    /// Set this attribute to the enumerable property for which you want the List<string> property to be generated in the DTO.
    /// It will be filled with display names using mapper. 
    /// It is used to display comma separated display names ​​in a table on the UI.
    /// </summary>
    public class GenerateCommaSeparatedDisplayNameAttribute : Attribute
    {
        public GenerateCommaSeparatedDisplayNameAttribute() { }
    }
}
