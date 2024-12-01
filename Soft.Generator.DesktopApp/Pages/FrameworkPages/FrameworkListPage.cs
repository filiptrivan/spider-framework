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
            FrameworkDetailsPage frameworkDetailsPage = _pageNavigator.NavigateToPage<FrameworkDetailsPage>(this);
            frameworkDetailsPage.Initialize(new Framework());
        }

        public void CellContentClickHandler(object sender, DataGridViewCellEventArgs e)
        {
            _clientSharedService.CellContentClickHandler<FrameworkDetailsPage, Framework, int>(
                e,
                this,
                softDataGridView1,
                _pageNavigator.NavigateToPage<FrameworkDetailsPage>,
                _frameworkController.GetFramework,
                _frameworkController.DeleteFramework,
                LoadTable
            );
        }
    }
}
