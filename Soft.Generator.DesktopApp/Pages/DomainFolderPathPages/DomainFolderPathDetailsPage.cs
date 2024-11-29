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

namespace Soft.Generator.DesktopApp.Pages.DomainFolderPathPages
{
    public partial class DomainFolderPathDetailsPage : UserControl
    {
        PageNavigator _pageNavigator;
        DomainFolderPathController _domainFolderPathController;
        ClientSharedService _clientSharedService;

        private DomainFolderPath Entity { get; set; } = new DomainFolderPath();

        public DomainFolderPathDetailsPage(DomainFolderPathController domainFolderPathController, PageNavigator pageNavigator, ClientSharedService clientSharedService)
        {
            _domainFolderPathController = domainFolderPathController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;

            InitializeComponent();
        }

        public void Initialize(DomainFolderPath entity)
        {
            Entity = entity;
            tb_Path.TextBoxValue = Entity.Path;
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            Entity = _domainFolderPathController.SaveDomainFolderPath(new DomainFolderPath
            {
                Id = Entity.Id,
                Path = tb_Path.TextBoxValue,
            });

            _clientSharedService.ShowSuccessfullMessage();
        }
    }
}
