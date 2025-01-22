using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF.UI
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class UIColumnAttribute : Attribute
    {
        public UIColumnAttribute(string field, string translateKey = null) { }
    }
}
