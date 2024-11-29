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
            _pageNavigator.NavigateToPage<DomainFolderPathDetailsPage>(this);
        }

        public void CellContentClickHandler(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewColumn detailsColumn = softDataGridView1.ColumnCollection["Details"];
            int id = (int)softDataGridView1.RowCollection[e.RowIndex].Cells["Id"].Value;

            if (detailsColumn != null && e.ColumnIndex == detailsColumn.Index)
            {
                DomainFolderPathDetailsPage domainFolderPathDetailsPage = _pageNavigator.NavigateToPage<DomainFolderPathDetailsPage>(this);
                domainFolderPathDetailsPage.Initialize(_domainFolderPathController.GetDomainFolderPath(id));
            }

            DataGridViewColumn deleteColumn = softDataGridView1.ColumnCollection["Delete"];

            if (deleteColumn != null && e.ColumnIndex == deleteColumn.Index)
            {
                DialogResult dialogResult = MessageBox.Show("Da li ste sigurni da želite da obrišete objekat?", "Potvrda brisanja", MessageBoxButtons.YesNoCancel);

                if (dialogResult == DialogResult.Yes)
                {
                    _domainFolderPathController.DeleteDomainFolderPath(id);
                    LoadTable();

                    _clientSharedService.ShowSuccessfullMessage();
                }
            }
        }
    }
}
