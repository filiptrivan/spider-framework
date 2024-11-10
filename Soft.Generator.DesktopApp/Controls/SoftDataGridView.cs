using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Entities;
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
    public partial class SoftDataGridView : UserControl
    {
        public SoftDataGridView()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;    
        }

        public void SoftInitializeComponent<T>(List<T> dataSource, bool showAddButton, EventHandler addNewHandler = null)
        {
            dataGridView1.DataSource = new BindingList<T>(dataSource);
            btn_AddNew.Enabled = showAddButton;
            btn_AddNew.Click += addNewHandler;
        }
    }
}
