using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Entities;
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
    public partial class WebApplicationListPage : UserControl
    {
        WebApplicationController _webApplicationController;
        PageNavigator _pageNavigator;
        ClientSharedService _clientSharedService;

        public WebApplicationListPage(WebApplicationController webApplicationController, PageNavigator pageNavigator, ClientSharedService clientSharedService)
        {
            _webApplicationController = webApplicationController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;

            InitializeComponent();

            softDataGridView1.SoftInitializeComponent<WebApplication>(_webApplicationController.GetWebApplicationList(), true, ApplicationAddEventHandler, true, true, CellContentClickHandler);
        }

        public void ApplicationAddEventHandler(object sender, EventArgs e)
        {
            WebApplicationDetailsPage applicationDetailsPage = _pageNavigator.NavigateToPage<WebApplicationDetailsPage>(this);
            applicationDetailsPage.Initialize(new WebApplication());
        }

        public void CellContentClickHandler(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewColumn detailsColumn = softDataGridView1.ColumnCollection["Details"];
            if (detailsColumn != null && e.ColumnIndex == detailsColumn.Index)
            {
                int id = (int)softDataGridView1.RowCollection[e.RowIndex].Cells["Id"].Value;

                WebApplicationDetailsPage applicationDetailsPage = _pageNavigator.NavigateToPage<WebApplicationDetailsPage>(this);
                applicationDetailsPage.Initialize(_webApplicationController.GetWebApplication(id));
            }
        }

    }
}
