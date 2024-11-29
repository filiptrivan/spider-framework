using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soft.Generator.DesktopApp.Controls
{
    public partial class SoftCheckbox : UserControl
    {
        public string LabelValue
        {
            get { return checkBox1.Text; }
            set { checkBox1.Text = value; }
        }

        public bool Value
        {
            get { return checkBox1.Checked; }
            set { checkBox1.Checked = value; }
        }

        public SoftCheckbox()
        {
            InitializeComponent();
        }
    }
}
