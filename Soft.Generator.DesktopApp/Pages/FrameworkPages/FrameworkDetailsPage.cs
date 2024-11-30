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
        ValidationService _validationService;

        private Framework Entity { get; set; } = new Framework();

        public FrameworkDetailsPage(FrameworkController frameworkController, PageNavigator pageNavigator, ClientSharedService clientSharedService, ValidationService validationService)
        {
            _frameworkController = frameworkController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;
            _validationService = validationService;

            InitializeComponent();
        }

        public void Initialize(Framework entity)
        {
            Entity = entity;
            tb_Name.TextBoxValue = Entity.Name;
            tb_Code.TextBoxValue = Entity.Code;
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            Entity = _frameworkController.SaveFramework(new Framework
            {
                Id = Entity.Id,
                Name = tb_Name.TextBoxValue,
                Code = tb_Code.TextBoxValue,
            });

            _clientSharedService.ShowSuccessfullMessage();
        }
    }
}
