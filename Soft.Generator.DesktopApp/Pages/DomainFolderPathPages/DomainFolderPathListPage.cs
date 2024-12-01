using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Pages.DomainFolderPathPages;
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
    public partial class DomainFolderPathListPage : UserControl
    {
        DomainFolderPathController _domainFolderPathController;
        PageNavigator _pageNavigator;
        ClientSharedService _clientSharedService;

        public DomainFolderPathListPage(DomainFolderPathController domainFolderPathController, PageNavigator pageNavigator, ClientSharedService clientSharedService)
        {
            _domainFolderPathController = domainFolderPathController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;

            InitializeComponent();

            LoadTable();
        }

        private void LoadTable()
        {
            softDataGridView1.SoftInitializeComponent<DomainFolderPath>(_domainFolderPathController.GetDomainFolderPathList(), true, DomainFolderPathAddEventHandler, true, true, CellContentClickHandler);
        }

        public void DomainFolderPathAddEventHandler(object sender, EventArgs e)
        {
            DomainFolderPathDetailsPage domainFolderPathDetailsPage = _pageNavigator.NavigateToPage<DomainFolderPathDetailsPage>(this);
            domainFolderPathDetailsPage.Initialize(new DomainFolderPath());
        }

        public void CellContentClickHandler(object sender, DataGridViewCellEventArgs e)
        {
            _clientSharedService.CellContentClickHandler<DomainFolderPathDetailsPage, DomainFolderPath, int>(
                e,
                this,
                softDataGridView1,
                _pageNavigator.NavigateToPage<DomainFolderPathDetailsPage>,
                _domainFolderPathController.GetDomainFolderPath,
                _domainFolderPathController.DeleteDomainFolderPath,
                LoadTable
            );
        }
    }
}
