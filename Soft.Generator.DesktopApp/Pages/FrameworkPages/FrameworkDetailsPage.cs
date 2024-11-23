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

namespace Soft.Generator.DesktopApp.Pages.FrameworkPages
{
    public partial class FrameworkDetailsPage : UserControl
    {
        PageNavigator _pageNavigator;
        FrameworkController _frameworkController;
        ClientSharedService _clientSharedService;

        public Framework entity = new Framework();

        public FrameworkDetailsPage(PageNavigator pageNavigator, FrameworkController frameworkController, ClientSharedService clientSharedService)
        {
            _pageNavigator = pageNavigator;
            _frameworkController = frameworkController;
            _clientSharedService = clientSharedService;

            InitializeComponent();
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            entity = _frameworkController.SaveFramework(new Framework
            {
                Id = entity.Id,
                Name = tb_Name.TextBoxValue,
                Code = tb_Code.TextBoxValue,
            });

            _clientSharedService.ShowSuccessfullMessage();
        }
    }
}
