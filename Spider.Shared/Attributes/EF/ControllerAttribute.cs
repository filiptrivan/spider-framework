using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    public class ControllerAttribute : Attribute
    {
        /// <summary>
        /// Set this attribute on the entities for which you do not want the controller to be called {entityName}Controller, but to give it a custom name and possibly connect more entities to that controller
        /// </summary>
        public ControllerAttribute(string controllerName) 
        {
            
        }
    }
}
