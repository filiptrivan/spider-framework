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

        private readonly ApplicationPage _applicationPage;
        private readonly CompanyPage _companyPage;
        private readonly FrameworkPage _frameworkPage;
        private readonly HomePage _homePage;
        private readonly PathToDomainFolderPage _pathToDomainFolderPage;
        private readonly PermissionPage _permissionPage;
        private readonly SettingPage _settingPage;

        #endregion

        private readonly SqlConnection _connection;
        private readonly DesktopAppBusinessService _desktopAppService;

        public Form1(SqlConnection connection, DesktopAppBusinessService desktopAppService, ApplicationPage applicationPage, CompanyPage companyPage, FrameworkPage frameworkPage, HomePage homePage,
            PathToDomainFolderPage pathToDomainFolderPage, PermissionPage permissionPage, SettingPage settingPage)
        {                                                                                   
            _applicationPage = applicationPage;                                             
            _companyPage = companyPage;                                                     
            _frameworkPage = frameworkPage;                                                 
            _homePage = homePage;                                                           
            _pathToDomainFolderPage = pathToDomainFolderPage;                               
            _permissionPage = permissionPage;
            _settingPage = settingPage;


            _connection = connection;
            _desktopAppService = desktopAppService;

            InitializeComponent();

            homeToolStripMenuItem_Click(null, null);
            //System.Windows.Forms.Form.WindowState = FormWindowState.Maximized;
            //Form1.WindowState = FormWindowState.Maximized;
        }

        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {

            _connection.WithTransaction(() =>
            {
                List<Permission> permissions = _desktopAppService.GetPermissionList();
                List<Permission> permissionsForCompany = _desktopAppService.GetPermissionListForCompanyList([1, 2]);
            });


            NavigateToPage(_homePage);
        }

        private void applicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage(_applicationPage);
        }

        private void companyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage(_companyPage);
        }

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage(_settingPage);
        }

        private void frameworkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage(_frameworkPage);
        }

        private void permissionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage(_permissionPage);
        }

        private void pathToDomainFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage(_pathToDomainFolderPage);
        }

        private void NavigateToPage(UserControl userControl)
        {
            pnl_Main.Controls.Clear();

            userControl.Dock = DockStyle.Fill;

            pnl_Main.Controls.Add(userControl);
        }

    }
}
