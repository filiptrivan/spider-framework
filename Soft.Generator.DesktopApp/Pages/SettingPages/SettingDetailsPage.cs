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

namespace Soft.Generator.DesktopApp.Pages.SettingPages
{
    public partial class SettingDetailsPage : UserControl, ISoftDetailsPage
    {
        PageNavigator _pageNavigator;
        SettingController _settingController;
        ClientSharedService _clientSharedService;
        ValidationService _validationService;

        private Setting Entity { get; set; } = new Setting();

        public SettingDetailsPage(SettingController settingController, PageNavigator pageNavigator, ClientSharedService clientSharedService, ValidationService validationService)
        {
            _settingController = settingController;
            _pageNavigator = pageNavigator;
            _clientSharedService = clientSharedService;
            _validationService = validationService;

            InitializeComponent();
        }

        public void Initialize(ISoftEntity entity)
        {
            Entity = (Setting)entity;

            tb_Name.TextBoxValue = Entity.Name;
            tb_Name.InvalidMessage = _validationService.SettingNameValidationMessage;

            tb_PrimaryColor.TextBoxValue = Entity.PrimaryColor;
            tb_PrimaryColor.InvalidMessage = _validationService.SettingPrimaryColorValidationMessage;

            chb_HasGoogleAuth.Value = Entity.HasGoogleAuth;
            chb_HasGoogleAuth.InvalidMessage = _validationService.SettingHasGoogleAuthValidationMessage;

            chb_HasLatinTranslate.Value = Entity.HasLatinTranslate;
            chb_HasLatinTranslate.InvalidMessage = _validationService.SettingHasLatinTranslateValidationMessage;

            chb_HasDarkMode.Value = Entity.HasDarkMode;
            chb_HasDarkMode.InvalidMessage = _validationService.SettingHasDarkModeValidationMessage;

            chb_HasNotifications.Value = Entity.HasNotifications;
            chb_HasNotifications.InvalidMessage = _validationService.SettingHasNotificationsValidationMessage;

            cb_Framework.SelectedValue = Entity.Framework?.Id ?? 0;
            cb_Framework.DisplayMember = nameof(Framework.Name);
            cb_Framework.InvalidMessage = _validationService.SettingFrameworkIdValidationMessage;
            cb_Framework.Initialize<Framework>(_settingController.GetFrameworkList());
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            Setting setting = new Setting
            {
                Id = Entity.Id,
                Name = tb_Name.TextBoxValue,
                HasGoogleAuth = chb_HasGoogleAuth.Value,
                PrimaryColor = tb_PrimaryColor.TextBoxValue,
                HasLatinTranslate = chb_HasLatinTranslate.Value,
                HasDarkMode = chb_HasDarkMode.Value,
                HasNotifications = chb_HasNotifications.Value,
                Framework = new Framework { Id = (int)cb_Framework.SelectedValue },
            };

            if (_validationService.IsSettingValid(setting) == false)
            {
                ValidateAllChildControls();
                return;
            }

            Entity = _settingController.SaveSetting(setting);

            _clientSharedService.ShowSuccessfullMessage();
        }

        public void ValidateAllChildControls()
        {
            tb_Name.StartValidation();
            cb_Framework.StartValidation();
            tb_PrimaryColor.StartValidation();
            chb_HasGoogleAuth.StartValidation();
            chb_HasLatinTranslate.StartValidation();
            chb_HasDarkMode.StartValidation();
            chb_HasNotifications.StartValidation();
        }
    }
}
