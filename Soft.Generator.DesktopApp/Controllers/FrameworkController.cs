using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Controllers
{
    public class FrameworkController
    {
        DesktopAppBusinessService _desktopAppBusinessService;

        public FrameworkController(DesktopAppBusinessService desktopAppBusinessService)
        {
            _desktopAppBusinessService = desktopAppBusinessService;
        }

        public Framework SaveFramework(Framework framework)
        {
            return _desktopAppBusinessService.SaveFramework(framework);
        }

        public List<Framework> GetFrameworkList()
        {
            return _desktopAppBusinessService.GetFrameworkList();
        }

        public Framework GetFramework(int id)
        {
            return _desktopAppBusinessService.GetFramework(id);
        }

        public void DeleteFramework(int id)
        {
            _desktopAppBusinessService.DeleteFramework(id);
        }
    }
}
