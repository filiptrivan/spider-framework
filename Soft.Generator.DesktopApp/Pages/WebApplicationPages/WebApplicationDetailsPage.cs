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

        private WebApplication Entity { get; set; } = new WebApplication();

        public WebApplicationDetailsPage(WebApplicationController webApplicationController, PageNavigator pageNavigator, ClientSharedService clientSharedService)
        {
            _webApplicationController = webApplicationController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;

            InitializeComponent();
        }

        public void Initialize(WebApplication entity)
        {
            Entity = entity;
            tb_Name.TextBoxValue = Entity.Name;

            cb_Company.Initialize<Company>(_webApplicationController.GetCompanyList());
            cb_Company.SelectedValue = Entity.Company.Id;
            cb_Company.DisplayMember = nameof(Company.Name);

            cb_Setting.Initialize<Setting>(_webApplicationController.GetSettingList());
            cb_Company.SelectedValue = Entity.Setting.Id;
            cb_Company.DisplayMember = nameof(Setting.Name);
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
                //Name = tb_Name.TextBoxValue,
                //Code = tb_Code.TextBoxValue,
            });

            _clientSharedService.ShowSuccessfullMessage();
        }
    }
}
