using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Controllers
{
    public class DomainFolderPathController
    {
        DesktopAppBusinessService _desktopAppBusinessService;

        public DomainFolderPathController(DesktopAppBusinessService desktopAppBusinessService)
        {
            _desktopAppBusinessService = desktopAppBusinessService;
        }

        public DomainFolderPath SaveDomainFolderPath(DomainFolderPath company)
        {
            return _desktopAppBusinessService.SaveDomainFolderPath(company);
        }

        public List<DomainFolderPath> GetDomainFolderPathList()
        {
            return _desktopAppBusinessService.GetDomainFolderPathList();
        }

        public DomainFolderPath GetDomainFolderPath(int id)
        {
            return _desktopAppBusinessService.GetDomainFolderPath(id);
        }

        public void DeleteDomainFolderPath(int id)
        {
            _desktopAppBusinessService.DeleteDomainFolderPath(id);
        }
    }
}
