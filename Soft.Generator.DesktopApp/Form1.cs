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
        private readonly SqlConnection _connection;
        private readonly DesktopAppBusinessService _desktopAppService;

        public Form1(SqlConnection connection, DesktopAppBusinessService desktopAppService)
        {
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
                Permission permission = _desktopAppService.GetPermission(1);
            });


            NavigateToPage<HomePage>();
        }

        private void applicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage<ApplicationPage>();
        }

        private void companyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage<CompanyPage>();
        }

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage<SettingPage>();
        }

        private void frameworkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage<FrameworkPage>();
        }

        private void permissionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage<PermissionPage>();
        }

        private void pathToDomainFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NavigateToPage<PathToDomainFolderPage>();
        }

        private void NavigateToPage<T>() where T : UserControl, new()
        {
            pnl_Main.Controls.Clear();

            T page = new T();

            page.Dock = DockStyle.Fill;

            pnl_Main.Controls.Add(page);
        }

    }
}
