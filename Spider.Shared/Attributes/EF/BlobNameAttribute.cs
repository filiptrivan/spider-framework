using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    /// <summary>
    /// Set this attribute to a property that serves as a pointer to the file identifier in azure storage.
    /// </summary>
    public class BlobNameAttribute : Attribute
    {
    }
}
