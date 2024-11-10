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
        #region Pages

        private readonly FrameworkDetailsPage _frameworkDetailsPage;

        #endregion

        FrameworkController _frameworkController;
        PageNavigator _pageNavigator;

        public FrameworkListPage(FrameworkController frameworkController, PageNavigator pageNavigator, FrameworkDetailsPage frameworkDetailsPage)
        {
            _frameworkController = frameworkController;
            _pageNavigator = pageNavigator;
            _frameworkDetailsPage = frameworkDetailsPage;

            InitializeComponent();

            softDataGridView1.SoftInitializeComponent<Framework>(_frameworkController.GetFrameworkList(), true, FrameworkAddEventHandler);
        }

        public void FrameworkAddEventHandler(object sender, EventArgs e)
        {
            _pageNavigator.NavigateToPage(_frameworkDetailsPage, this);
        }
    }
}
