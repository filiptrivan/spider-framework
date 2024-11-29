using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Controllers
{
    public class CompanyController
    {
        DesktopAppBusinessService _desktopAppBusinessService;

        public CompanyController(DesktopAppBusinessService desktopAppBusinessService)
        {
            _desktopAppBusinessService = desktopAppBusinessService;
        }

        public Company SaveCompany(Company company)
        {
            return _desktopAppBusinessService.SaveCompany(company);
        }

        public List<Company> GetCompanyList()
        {
            return _desktopAppBusinessService.GetCompanyList();
        }

        public Company GetCompany(int id)
        {
            return _desktopAppBusinessService.GetCompany(id);
        }

        public void DeleteCompany(int id)
        {
            _desktopAppBusinessService.DeleteCompany(id);
        }
    }
}
