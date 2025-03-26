using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ProjectToDTOAttribute : Attribute
    {
        public ProjectToDTOAttribute(string customMapper) { }
    }
}
