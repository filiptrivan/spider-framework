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
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Soft.Generator.DesktopApp.Pages.CompanyPages
{
    public partial class CompanyDetailsPage : UserControl
    {
        PageNavigator _pageNavigator;
        CompanyController _companyController;
        ClientSharedService _clientSharedService;

        private Company Entity { get; set; } = new Company();

        public CompanyDetailsPage(CompanyController companyController, PageNavigator pageNavigator, ClientSharedService clientSharedService)
        {
            _companyController = companyController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;

            InitializeComponent();
        }

        public void Initialize(Company entity)
        {
            Entity = entity;
            tb_Name.TextBoxValue = Entity.Name;
            tb_Email.TextBoxValue = Entity.Email;
            tb_Password.TextBoxValue = Entity.Password;
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            Entity = _companyController.SaveCompany(new Company
            {
                Id = Entity.Id,
                Name = tb_Name.TextBoxValue,
                Email = tb_Email.TextBoxValue,
                Password = tb_Password.TextBoxValue,
            });

            _clientSharedService.ShowSuccessfullMessage();
        }
    }
}
