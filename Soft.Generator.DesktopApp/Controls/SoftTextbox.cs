using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soft.Generator.DesktopApp
{
    public partial class SoftTextbox : UserControl
    {
        public SoftTextbox()
        {
            InitializeComponent();
        }

        public string TextBoxValue
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }

        public string LabelValue
        {
            get { return label1.Text; }
            set { label1.Text = value; }
        }
    }
}
