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
    public partial class WebApplicationDetailsPage : UserControl
    {
        PageNavigator _pageNavigator;
        WebApplicationController _webApplicationController;
        ClientSharedService _clientSharedService;
        ValidationService _validationService;

        private WebApplication Entity { get; set; } = new WebApplication();

        public WebApplicationDetailsPage(WebApplicationController webApplicationController, PageNavigator pageNavigator, ClientSharedService clientSharedService, ValidationService validationService)
        {
            _webApplicationController = webApplicationController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;
            _validationService = validationService;

            InitializeComponent();
        }

        public void Initialize(WebApplication entity)
        {
            Entity = entity;
            tb_Name.TextBoxValue = Entity.Name;
            tb_Name.InvalidMessage = _validationService.WebApplicationNameValidationMessage;

            cb_Company.SelectedValue = Entity.Company?.Id ?? 0;
            cb_Company.DisplayMember = nameof(Company.Name);
            cb_Company.InvalidMessage = _validationService.WebApplicationCompanyIdValidationMessage;
            cb_Company.Initialize<Company>(_webApplicationController.GetCompanyList());

            cb_Setting.SelectedValue = Entity.Setting?.Id ?? 0;
            cb_Setting.DisplayMember = nameof(Setting.Name);
            cb_Company.InvalidMessage = _validationService.WebApplicationSettingIdValidationMessage;
            cb_Setting.Initialize<Setting>(_webApplicationController.GetSettingList());
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            Entity = _webApplicationController.SaveWebApplication(new WebApplication
            {
                Id = Entity.Id,
                Name = tb_Name.TextBoxValue,
                Company = cb_Company.SelectedValue == null ? null : new Company { Id = (int)cb_Company.SelectedValue },
                Setting = cb_Setting.SelectedValue == null ? null : new Setting { Id = (int)cb_Setting.SelectedValue },
            });

            _clientSharedService.ShowSuccessfullMessage();
        }
    }
}
