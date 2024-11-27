using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Pages.FrameworkPages;
using Soft.Generator.DesktopApp.Services;
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
    public partial class FrameworkListPage : UserControl
    {
        FrameworkController _frameworkController;
        PageNavigator _pageNavigator;
        ClientSharedService _clientSharedService;

        public FrameworkListPage(FrameworkController frameworkController, PageNavigator pageNavigator, ClientSharedService clientSharedService)
        {
            _frameworkController = frameworkController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;

            InitializeComponent();

            LoadTable();
        }

        private void LoadTable()
        {
            softDataGridView1.SoftInitializeComponent<Framework>(_frameworkController.GetFrameworkList(), true, FrameworkAddEventHandler, true, true, CellContentClickHandler);
        }

        public void FrameworkAddEventHandler(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage<FrameworkDetailsPage>(this);
        }

        public void CellContentClickHandler(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewColumn detailsColumn = softDataGridView1.ColumnCollection["Details"];
            int id = (int)softDataGridView1.RowCollection[e.RowIndex].Cells["Id"].Value;

            if (detailsColumn != null && e.ColumnIndex == detailsColumn.Index)
            {
                FrameworkDetailsPage frameworkDetailsPage = _pageNavigator.NavigateToPage<FrameworkDetailsPage>(this);
                frameworkDetailsPage.Initialize(_frameworkController.GetFramework(id));
            }

            DataGridViewColumn deleteColumn = softDataGridView1.ColumnCollection["Delete"];

            if (deleteColumn != null && e.ColumnIndex == deleteColumn.Index)
            {
                DialogResult dialogResult = MessageBox.Show("Da li ste sigurni da želite da obrišete objekat?", "Potvrda brisanja", MessageBoxButtons.YesNoCancel);

                if (dialogResult == DialogResult.Yes)
                {
                    _frameworkController.DeleteFramework(id);
                    LoadTable();

                    _clientSharedService.ShowSuccessfullMessage();
                }
            }
        }
    }
}
