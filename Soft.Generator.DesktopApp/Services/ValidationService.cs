using Soft.Generator.DesktopApp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Services
{
    public class ValidationService
    {
        public ValidationService() { }

        #region Company

        public bool IsCompanyValid(Company company)
        {
            if (CompanyNameValidationMessage(company.Name) != "")
                return false;

            if (CompanyEmailValidationMessage(company.Name) != "")
                return false;

            if (CompanyPasswordValidationMessage(company.Name) != "")
                return false;

            return true;
        }

        public string CompanyNameValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value)) 
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 500)
                errors.Add("Maksimalan broj karaktera za ovo polje je 500.");

            return string.Join(" ", errors);
        }

        public string CompanyEmailValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 500)
                errors.Add("Maksimalan broj karaktera za ovo polje je 400.");

            return string.Join(" ", errors);
        }

        public string CompanyPasswordValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 100)
                errors.Add("Maksimalan broj karaktera za ovo polje je 100.");

            return string.Join(" ", errors);
        }

        #endregion

        #region DomainFolderPath

        public bool IsDomainFolderPathValid(DomainFolderPath domainFolderPath)
        {
            if (DomainFolderPathPathValidationMessage(domainFolderPath.Path) != "")
                return false;

            return true;
        }

        public string DomainFolderPathPathValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 1000)
                errors.Add("Maksimalan broj karaktera za ovo polje je 1000.");

            return string.Join(" ", errors);
        }

        #endregion

        #region Framework

        public bool IsFrameworkValid(Framework framework)
        {
            if (FrameworkNameValidationMessage(framework.Name) != "")
                return false;

            if (FrameworkCodeValidationMessage(framework.Code) != "")
                return false;

            return true;
        }

        public string FrameworkNameValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 500)
                errors.Add("Maksimalan broj karaktera za ovo polje je 500.");

            return string.Join(" ", errors);
        }

        public string FrameworkCodeValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 500)
                errors.Add("Maksimalan broj karaktera za ovo polje je 500.");

            return string.Join(" ", errors);
        }

        #endregion

        #region Permission

        public bool IsPermissionValid(Permission permission)
        {
            if (PermissionNameValidationMessage(permission.Name) != "")
                return false;

            if (PermissionCodeValidationMessage(permission.Code) != "")
                return false;

            return true;
        }

        public string PermissionNameValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 100)
                errors.Add("Maksimalan broj karaktera za ovo polje je 100.");

            return string.Join(" ", errors);
        }

        public string PermissionCodeValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 100)
                errors.Add("Maksimalan broj karaktera za ovo polje je 100.");

            return string.Join(" ", errors);
        }

        #endregion

        #region Setting

        public bool IsSettingValid(Setting setting)
        {
            if (SettingNameValidationMessage(setting.Name) != "")
                return false;

            if (SettingPrimaryColorValidationMessage(setting.PrimaryColor) != "")
                return false;

            if (SettingHasLatinTranslateValidationMessage(setting.HasLatinTranslate) != "")
                return false;

            if (SettingHasDarkModeValidationMessage(setting.HasDarkMode) != "")
                return false;

            if (SettingHasNotificationsValidationMessage(setting.HasNotifications) != "")
                return false;

            if (SettingHasGoogleAuthValidationMessage(setting.HasGoogleAuth) != "")
                return false;

            if (SettingFrameworkIdValidationMessage(setting.Framework?.Id) != "")
                return false;

            return true;
        }

        public string SettingNameValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 500)
                errors.Add("Maksimalan broj karaktera za ovo polje je 500.");

            return string.Join(" ", errors);
        }

        public string SettingPrimaryColorValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length != 7)
                errors.Add("Polje mora imati tačno 7 karaktera.");

            return string.Join(" ", errors);
        }

        public string SettingHasLatinTranslateValidationMessage(bool? value)
        {
            if (value == null)
                return "Polje mora biti postavljeno (true ili false).";

            return string.Empty;
        }

        public string SettingHasDarkModeValidationMessage(bool? value)
        {
            if (value == null)
                return "Polje mora biti postavljeno (true ili false).";

            return string.Empty;
        }

        public string SettingHasNotificationsValidationMessage(bool? value)
        {
            if (value == null)
                return "Polje mora biti postavljeno (true ili false).";

            return string.Empty;
        }

        public string SettingHasGoogleAuthValidationMessage(bool? value)
        {
            if (value == null)
                return "Polje mora biti postavljeno (true ili false).";

            return string.Empty;
        }

        public string SettingFrameworkIdValidationMessage(object? value)
        {
            if (value == null)
                return "Polje ne sme biti prazno.";

            return string.Empty;
        }

        #endregion

        #region WebApplication

        public bool IsWebApplicationValid(WebApplication webApplication)
        {
            if (WebApplicationNameValidationMessage(webApplication.Name) != "")
                return false;

            if (WebApplicationCompanyIdValidationMessage(webApplication.Company?.Id) != "")
                return false;

            if (WebApplicationSettingIdValidationMessage(webApplication.Setting?.Id) != "")
                return false;

            return true;
        }

        public string WebApplicationNameValidationMessage(string value)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(value))
                errors.Add("Polje ne sme biti prazno.");

            if (value.Length > 500)
                errors.Add("Maksimalan broj karaktera za ovo polje je 500.");

            return string.Join(" ", errors);
        }

        public string WebApplicationCompanyIdValidationMessage(object? value)
        {
            if (value == null)
                return "Polje ne sme biti prazno.";

            return string.Empty;
        }

        public string WebApplicationSettingIdValidationMessage(object? value)
        {
            if (value == null)
                return "Polje ne sme biti prazno.";

            return string.Empty;
        }

        #endregion
    }
}
