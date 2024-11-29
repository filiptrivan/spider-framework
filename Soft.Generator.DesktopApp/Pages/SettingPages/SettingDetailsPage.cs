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

namespace Soft.Generator.DesktopApp.Pages.SettingPages
{
    public partial class SettingDetailsPage : UserControl
    {
        PageNavigator _pageNavigator;
        SettingController _settingController;
        ClientSharedService _clientSharedService;

        private Setting Entity { get; set; } = new Setting();

        public SettingDetailsPage(SettingController settingController, PageNavigator pageNavigator, ClientSharedService clientSharedService)
        {
            _settingController = settingController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;

            InitializeComponent();
        }

        public void Initialize(Setting entity)
        {
            Entity = entity;

            cb_Framework.Initialize<Framework>(_settingController.GetFrameworkList());
            cb_Framework.SelectedValue = Entity.Framework.Id;
            cb_Framework.DisplayMember = nameof(Framework.Name);

            tb_Name.TextBoxValue = Entity.Name;
            chb_HasGoogleAuth.Value = Entity.HasGoogleAuth;
            tb_PrimaryColor.TextBoxValue = Entity.PrimaryColor;
            chb_HasLatinTranslate.Value = Entity.HasLatinTranslate;
            chb_HasDarkMode.Value = Entity.HasDarkMode;
            chb_HasNotifications.Value = Entity.HasNotifications;
            cb_Framework.SelectedValue = Entity.Framework.Id;
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            Entity = _settingController.SaveSetting(new Setting
            {
                Id = Entity.Id,
                Name = tb_Name.TextBoxValue,
                HasGoogleAuth = chb_HasGoogleAuth.Value,
                PrimaryColor = tb_PrimaryColor.TextBoxValue,
                HasLatinTranslate = chb_HasLatinTranslate.Value,
                HasDarkMode = chb_HasDarkMode.Value,
                HasNotifications = chb_HasNotifications.Value,
                Framework = new Framework { Id = (int)cb_Framework.SelectedValue },
            });

            _clientSharedService.ShowSuccessfullMessage();
        }
    }
}
