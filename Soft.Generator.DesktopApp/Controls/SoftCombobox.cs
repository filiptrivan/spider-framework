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

namespace Soft.Generator.DesktopApp.Controls
{
    public partial class SoftCombobox : UserControl
    {
        public string LabelValue
        {
            get { return label1.Text; }
            set { label1.Text = value; }
        }

        public object? SelectedValue
        {
            get { return comboBox1.SelectedValue; }
            set { comboBox1.SelectedValue = value; }
        }

        public string DisplayMember
        {
            get { return comboBox1.DisplayMember; }
            set { comboBox1.DisplayMember = value; }
        }

        public Func<object?, string> InvalidMessage { get; set; }

        public SoftCombobox()
        {
            InitializeComponent();

            comboBox1.Validating += comboBox1_Validating;

            comboBox1.ValueMember = "Id";
            comboBox1.SelectedIndex = -1;
        }

        public void Initialize<T>(
            List<T> dataSource,
            EventHandler selectedValueChangedHandler = null
        )
        {
            //comboBox1.DataSource = new BindingList<T>(dataSource);
            comboBox1.DataSource = dataSource;

            comboBox1.SelectedValueChanged += selectedValueChangedHandler;

            comboBox1.SelectedIndex = -1; // FT: Always the last
        }

        private void comboBox1_Validating(object sender, CancelEventArgs e)
        {
            if (InvalidMessage != null)
            {
                if (InvalidMessage(comboBox1.SelectedValue) == "")
                {
                    errorProvider1.SetError(comboBox1, null);
                }
                else
                {
                    errorProvider1.SetError(comboBox1, InvalidMessage(comboBox1.SelectedValue));
                }
            }
        }

        public void StartValidation()
        {
            var cancelEventArgs = new CancelEventArgs();
            comboBox1_Validating(comboBox1, cancelEventArgs);
        }
    }
}
