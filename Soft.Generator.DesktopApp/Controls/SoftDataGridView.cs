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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Soft.Generator.DesktopApp.Controls
{
    public partial class SoftDataGridView : UserControl
    {
        public DataGridViewColumnCollection ColumnCollection {
            get { return dataGridView1.Columns; }
        }

        public DataGridViewRowCollection RowCollection
        {
            get { return dataGridView1.Rows; }
        }

        public SoftDataGridView()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
        }

        public void SoftInitializeComponent<T>(
            List<T> dataSource, 
            bool showAddButton, 
            EventHandler addNewHandler = null, 
            bool showDetailsButton = true, 
            bool showDeleteButton = true,
            DataGridViewCellEventHandler cellContentClickHandler = null
        )
        {
            dataGridView1.DataSource = new BindingList<T>(dataSource);

            btn_AddNew.Enabled = showAddButton;
            btn_AddNew.Click += addNewHandler;

            if (showDetailsButton == false)
            {
                dataGridView1.Columns["Details"].Visible = false;
            }

            if (showDeleteButton == false)
            {
                dataGridView1.Columns["Delete"].Visible = false;
            }

            dataGridView1.CellContentClick += cellContentClickHandler;
        }

    }
}
