using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Soft.Generator.DesktopApp
{
    public partial class SoftTextbox : UserControl
    {
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

        public Func<string, string> InvalidMessage { get; set; }

        public SoftTextbox()
        {
            InitializeComponent();

            textBox1.Validating += textBox1_Validating;
        }

        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            if (InvalidMessage != null)
            {
                if (InvalidMessage(textBox1.Text) == null)
                {
                    errorProvider1.SetError(textBox1, null);
                }
                else
                {
                    errorProvider1.SetError(textBox1, InvalidMessage(textBox1.Text));
                }
            }
        }
    }
}
