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

            softDataGridView1.SoftInitializeComponent<Framework>(_frameworkController.GetFrameworkList(), true, FrameworkAddEventHandler, true, true, CellContentClickHandler);
        }

        public void FrameworkAddEventHandler(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage<FrameworkDetailsPage>(this);
        }

        public void CellContentClickHandler(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewColumn detailsColumn = softDataGridView1.ColumnCollection["Details"];
            if (detailsColumn != null && e.ColumnIndex == detailsColumn.Index)
            {
                long id = (long)softDataGridView1.RowCollection[e.RowIndex].Cells["Details"].Value;

                FrameworkDetailsPage frameworkDetailsPage = _pageNavigator.NavigateToPage<FrameworkDetailsPage>(this);
                frameworkDetailsPage.ObjectId = 
            }
        }
    }
}
