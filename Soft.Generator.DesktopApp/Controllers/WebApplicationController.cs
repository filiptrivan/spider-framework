using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Controllers
{
    public class WebApplicationController
    {
        DesktopAppBusinessService _desktopAppBusinessService;

        public WebApplicationController(DesktopAppBusinessService desktopAppBusinessService)
        {
            _desktopAppBusinessService = desktopAppBusinessService;
        }

        public WebApplication SaveWebApplication(WebApplication entity)
        {
            return _desktopAppBusinessService.SaveWebApplication(entity);
        }

        public void DeleteWebApplication(long id)
        {
            _desktopAppBusinessService.DeleteWebApplication(id);
        }

        public List<WebApplication> GetWebApplicationList()
        {
            return _desktopAppBusinessService.GetWebApplicationList();
        }

        public WebApplication GetWebApplication(long id)
        {
            return _desktopAppBusinessService.GetWebApplication(id);
        }

        public List<Company> GetCompanyList()
        {
            return _desktopAppBusinessService.GetCompanyList();
        }

        public List<Setting> GetSettingList()
        {
            return _desktopAppBusinessService.GetSettingList();
        }
    }
}
