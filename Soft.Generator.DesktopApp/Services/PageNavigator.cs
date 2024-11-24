using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Pages;
using Soft.Generator.DesktopApp.Pages.FrameworkPages;
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
        private readonly PathToDomainFolderController _pathToDomainFolderController;
        private readonly PermissionController _permissionController;
        private readonly SettingController _settingController;

        private readonly ClientSharedService _clientSharedService;

        private Stack<UserControl> _pageStack = new Stack<UserControl>();
        private Panel _pnl_Main;

        public PageNavigator(
            ClientSharedService clientSharedService,
            CompanyController companyController, FrameworkController frameworkController, HomeController homeController,
            PathToDomainFolderController pathToDomainFolderController, PermissionController permissionController, SettingController settingController, WebApplicationController webApplicationController
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
                return new CompanyListPage() as T;
            }
            else if (typeof(T) == typeof(FrameworkListPage))
            {
                return new FrameworkListPage(_frameworkController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(FrameworkDetailsPage))
            {
                return new FrameworkDetailsPage(_frameworkController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(HomePage))
            {
                return new HomePage() as T;
            }
            else if (typeof(T) == typeof(PathToDomainFolderListPage))
            {
                return new PathToDomainFolderListPage() as T;
            }
            else if (typeof(T) == typeof(PermissionListPage))
            {
                return new PermissionListPage(_permissionController) as T;
            }
            else if (typeof(T) == typeof(SettingListPage))
            {
                return new SettingListPage() as T;
            }
            else if (typeof(T) == typeof(WebApplicationListPage))
            {
                return new WebApplicationListPage(_webApplicationController, this, _clientSharedService) as T;
            }
            else if (typeof(T) == typeof(WebApplicationDetailsPage))
            {
                return new WebApplicationDetailsPage(_webApplicationController, this, _clientSharedService) as T;
            }

            throw new NotSupportedException("Niste napravili stranu za prikaz.");
        }

        public UserControl InstantiatePage(string pageName)
        {
            switch (pageName)
            {
                case nameof(HomePage):
                    return new HomePage();
                case nameof(FrameworkListPage):
                    return new FrameworkListPage(_frameworkController, this, _clientSharedService);
                case nameof(FrameworkDetailsPage):
                    return new FrameworkDetailsPage(_frameworkController, this, _clientSharedService);
            }

            throw new NotSupportedException("Niste napravili stranu za prikaz.");
        }
    }
}
