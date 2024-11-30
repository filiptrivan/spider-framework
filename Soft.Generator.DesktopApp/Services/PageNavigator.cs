using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Pages;
using Soft.Generator.DesktopApp.Pages.CompanyPages;
using Soft.Generator.DesktopApp.Pages.DomainFolderPathPages;
using Soft.Generator.DesktopApp.Pages.FrameworkPages;
using Soft.Generator.DesktopApp.Pages.SettingPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Services
{
    public class PageNavigator
    {
        private readonly WebApplicationController _webApplicationController;
        private readonly CompanyController _companyController;
        private readonly FrameworkController _frameworkController;
        private readonly HomeController _homeController;
        private readonly DomainFolderPathController _pathToDomainFolderController;
        private readonly PermissionController _permissionController;
        private readonly SettingController _settingController;

        private readonly ClientSharedService _clientSharedService;
        private readonly ValidationService _validationService;

        private Stack<UserControl> _pageStack = new Stack<UserControl>();
        private Panel _pnl_Main;

        public PageNavigator(
            ClientSharedService clientSharedService,
            CompanyController companyController, FrameworkController frameworkController, HomeController homeController,
            DomainFolderPathController pathToDomainFolderController, PermissionController permissionController, SettingController settingController, WebApplicationController webApplicationController,
            ValidationService validationService
            )
        {
            _companyController = companyController;
            _frameworkController = frameworkController;
            _homeController = homeController;
            _pathToDomainFolderController = pathToDomainFolderController;
            _permissionController = permissionController;
            _settingController = settingController;
            _webApplicationController = webApplicationController;

            _clientSharedService = clientSharedService;
            _validationService = validationService;
        }

        public void InitializeMainPanel(Panel pnl_Main)
        {
            _pnl_Main = pnl_Main;
        }

        /// <summary>
        /// Pass <paramref name="currentPage"/> if you want to have the return button on the page which you are navigating to.
        /// </summary>
        public T NavigateToPage<T>(UserControl currentPage = null) where T : UserControl
        {
            _pnl_Main.Controls.Clear();

            T page = InstantiatePage<T>();

            page.Dock = DockStyle.Fill;

            _pnl_Main.Controls.Add(page);

            if (currentPage != null && (_pageStack.Count == 0 || _pageStack.Peek() != currentPage))
            {
                _pageStack.Push(currentPage);
            }

            return page;
        }

        public void NavigateBack()
        {
            if (_pageStack.Count > 0)
            {
                UserControl previousPage = _pageStack.Pop();

                _pnl_Main.Controls.Clear();

                UserControl reInstantiatedPreviousPage = InstantiatePage(previousPage.Name);

                reInstantiatedPreviousPage.Dock = DockStyle.Fill;

                _pnl_Main.Controls.Add(reInstantiatedPreviousPage);
            }
        }

        public T InstantiatePage<T>() where T : UserControl
        {
            if (typeof(T) == typeof(CompanyListPage))
            {
                return new CompanyListPage(_companyController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(CompanyDetailsPage))
            {
                return new CompanyDetailsPage(_companyController, this, _clientSharedService, _validationService) as T;
            }
            else if (typeof(T) == typeof(FrameworkListPage))
            {
                return new FrameworkListPage(_frameworkController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(FrameworkDetailsPage))
            {
                return new FrameworkDetailsPage(_frameworkController, this, _clientSharedService, _validationService) as T;
            }
            else if (typeof(T) == typeof(HomePage))
            {
                return new HomePage() as T;
            }
            else if (typeof(T) == typeof(DomainFolderPathListPage))
            {
                return new DomainFolderPathListPage(_pathToDomainFolderController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(DomainFolderPathDetailsPage))
            {
                return new DomainFolderPathDetailsPage(_pathToDomainFolderController, this, _clientSharedService, _validationService) as T;
            }
            else if (typeof(T) == typeof(PermissionListPage))
            {
                return new PermissionListPage(_permissionController) as T;
            }
            else if (typeof(T) == typeof(SettingListPage))
            {
                return new SettingListPage(_settingController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(SettingDetailsPage))
            {
                return new SettingDetailsPage(_settingController, this, _clientSharedService, _validationService) as T;
            }
            else if (typeof(T) == typeof(WebApplicationListPage))
            {
                return new WebApplicationListPage(_webApplicationController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(WebApplicationDetailsPage))
            {
                return new WebApplicationDetailsPage(_webApplicationController, this, _clientSharedService, _validationService) as T;
            }

            throw new NotSupportedException("Niste napravili stranu za prikaz.");
        }

        public UserControl InstantiatePage(string pageName)
        {
            switch (pageName)
            {
                case nameof(CompanyListPage):
                    return new CompanyListPage(_companyController, this, _clientSharedService);
                case nameof(CompanyDetailsPage):
                    return new CompanyDetailsPage(_companyController, this, _clientSharedService, _validationService);
                case nameof(FrameworkListPage):
                    return new FrameworkListPage(_frameworkController, this, _clientSharedService);
                case nameof(FrameworkDetailsPage):
                    return new FrameworkDetailsPage(_frameworkController, this, _clientSharedService, _validationService);
                case nameof(HomePage):
                    return new HomePage();
                case nameof(DomainFolderPathListPage):
                    return new DomainFolderPathListPage(_pathToDomainFolderController, this, _clientSharedService);
                case nameof(DomainFolderPathDetailsPage):
                    return new DomainFolderPathDetailsPage(_pathToDomainFolderController, this, _clientSharedService, _validationService);
                case nameof(PermissionListPage):
                    return new PermissionListPage(_permissionController);
                case nameof(SettingListPage):
                    return new SettingListPage(_settingController, this, _clientSharedService);
                case nameof(SettingDetailsPage):
                    return new SettingDetailsPage(_settingController, this, _clientSharedService, _validationService);
                case nameof(WebApplicationListPage):
                    return new WebApplicationListPage(_webApplicationController, this, _clientSharedService);
                case nameof(WebApplicationDetailsPage):
                    return new WebApplicationDetailsPage(_webApplicationController, this, _clientSharedService, _validationService);
            }

            throw new NotSupportedException("Niste napravili stranu za prikaz.");
        }
    }
}
