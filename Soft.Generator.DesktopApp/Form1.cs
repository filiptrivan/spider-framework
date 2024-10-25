using Soft.Generator.DesktopApp.Pages;

namespace Soft.Generator.DesktopApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //System.Windows.Forms.Form.WindowState = FormWindowState.Maximized;
            //Form1.WindowState = FormWindowState.Maximized;
        }

        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pnl_Main.Controls.Clear();

            HomePage homePage = new HomePage();

            homePage.Dock = DockStyle.Fill;

            pnl_Main.Controls.Add(homePage);
        }

        private void applicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pnl_Main.Controls.Clear();

            ApplicationPage applicationPage = new ApplicationPage();

            applicationPage.Dock = DockStyle.Fill;

            pnl_Main.Controls.Add(applicationPage);
        }
    }
}
