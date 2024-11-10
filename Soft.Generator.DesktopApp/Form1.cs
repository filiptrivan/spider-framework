using Microsoft.Data.SqlClient;
using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Extensions;
using Soft.Generator.DesktopApp.Pages;
using Soft.Generator.DesktopApp.Services;
using System.Data;

namespace Soft.Generator.DesktopApp
{
    public partial class Form1 : Form
    {
        #region Pages

        private readonly ApplicationListPage _applicationPage;
        private readonly CompanyListPage _companyPage;
        private readonly FrameworkListPage _frameworkPage;
        private readonly HomePage _homePage;
        private readonly PathToDomainFolderListPage _pathToDomainFolderPage;
        private readonly PermissionListPage _permissionPage;
        private readonly SettingListPage _settingPage;

        #endregion

        private readonly DesktopAppBusinessService _desktopAppService;
        private readonly PageNavigator _pageNavigator;

        public Form1(
            DesktopAppBusinessService desktopAppService, PageNavigator pageNavigator, ApplicationListPage applicationPage, CompanyListPage companyPage,
            FrameworkListPage frameworkPage, HomePage homePage, PathToDomainFolderListPage pathToDomainFolderPage, PermissionListPage permissionPage,
            SettingListPage settingPage
            )
        {
            _applicationPage = applicationPage;
            _companyPage = companyPage;
            _frameworkPage = frameworkPage;
            _homePage = homePage;
            _pathToDomainFolderPage = pathToDomainFolderPage;
            _permissionPage = permissionPage;
            _settingPage = settingPage;

            _desktopAppService = desktopAppService;
            _pageNavigator = pageNavigator;

            InitializeComponent();

            _pageNavigator.InitializeMainPanel(pnl_Main);
            homeToolStripMenuItem_Click(null, null);
            //System.Windows.Forms.Form.WindowState = FormWindowState.Maximized;
            //Form1.WindowState = FormWindowState.Maximized;
        }

        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_homePage);
        }

        private void applicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_applicationPage);
        }

        private void companyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_companyPage);
        }

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_settingPage);
        }

        private void frameworkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_frameworkPage);
        }

        private void permissionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_permissionPage);
        }

        private void pathToDomainFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_pathToDomainFolderPage);
        }

    }
}
