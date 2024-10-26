using MySql.Data.MySqlClient;
using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Pages;
using System.Data;

namespace Soft.Generator.DesktopApp
{
    public partial class Form1 : Form
    {
        MySqlConnection connection = new MySqlConnection(Settings.ConnectionString);

        public Form1()
        {
            InitializeComponent();

            homeToolStripMenuItem_Click(null, null);
            //System.Windows.Forms.Form.WindowState = FormWindowState.Maximized;
            //Form1.WindowState = FormWindowState.Maximized;
        }

        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Permission> permissions = GetPermissions();

            NavigateToPage<HomePage>();
        }

        private List<Permission> GetPermissions()
        {
            List<Permission> permissions = new List<Permission>();

            string query = "SELECT * FROM Permission";

            try
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var permission = new Permission
                        {
                            Id = reader.GetInt32("Id"),
                            Name = reader.GetString("Name"),
                            Code = reader.GetString("Code"),
                        };
                        permissions.Add(permission);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                connection.Close();
            }

            return permissions;
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
