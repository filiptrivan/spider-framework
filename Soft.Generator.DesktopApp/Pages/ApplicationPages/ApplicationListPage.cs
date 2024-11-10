using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soft.Generator.DesktopApp.Pages
{
    public partial class ApplicationListPage : UserControl
    {
        public ApplicationListPage()
        {
            InitializeComponent();
        }

        private void softTextbox1_Load(object sender, EventArgs e)
        {
            softTextbox1.LabelValue = "Ime aplikacije";
        }
    }
}
