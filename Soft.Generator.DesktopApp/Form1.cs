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

        private void softTextbox1_Load(object sender, EventArgs e)
        {
            softTextbox1.LabelValue = "Ime aplikacije";
        }

        private void početnaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pnl_Main.Controls.Clear();

            HomePage pocetnaPage = new HomePage();

            pocetnaPage.Dock = DockStyle.Fill;

            pnl_Main.Controls.Add(pocetnaPage);
        }
    }
}
