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

        public string DisplayMember
        {
            get { return comboBox1.DisplayMember; }
            set { comboBox1.DisplayMember = value; }
        }

        public SoftCombobox()
        {
            InitializeComponent();

            comboBox1.ValueMember = "Id";
            comboBox1.SelectedIndex = -1;
        }

        public void Initialize<T>(
            List<T> dataSource,
            EventHandler selectedValueChangedHandler = null
        )
        {
            comboBox1.DataSource = new BindingList<T>(dataSource);

            comboBox1.SelectedValueChanged += selectedValueChangedHandler;
        }
    }
}
