using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Interfaces;
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

namespace Soft.Generator.DesktopApp.Pages.DomainFolderPathPages
{
    public partial class DomainFolderPathDetailsPage : UserControl, ISoftDetailsPage
    {
        PageNavigator _pageNavigator;
        DomainFolderPathController _domainFolderPathController;
        ClientSharedService _clientSharedService;
        ValidationService _validationService;

        private DomainFolderPath Entity { get; set; } = new DomainFolderPath();

        public DomainFolderPathDetailsPage(DomainFolderPathController domainFolderPathController, PageNavigator pageNavigator, ClientSharedService clientSharedService, ValidationService validationService)
        {
            _domainFolderPathController = domainFolderPathController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;
            _validationService = validationService;

            InitializeComponent();
        }

        public void Initialize(ISoftEntity entity)
        {
            Entity = (DomainFolderPath)entity;

            tb_Path.TextBoxValue = Entity.Path;
            tb_Path.InvalidMessage = _validationService.DomainFolderPathPathValidationMessage;
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            DomainFolderPath domainFolderPath = new DomainFolderPath
            {
                Id = Entity.Id,
                Path = tb_Path.TextBoxValue,
            };

            if (_validationService.IsDomainFolderPathValid(domainFolderPath) == false)
            {
                ValidateAllChildControls();
                return;
            }

            Entity = _domainFolderPathController.SaveDomainFolderPath(domainFolderPath);

            _clientSharedService.ShowSuccessfullMessage();
        }

        public void ValidateAllChildControls()
        {
            tb_Path.StartValidation();
        }
    }
}
