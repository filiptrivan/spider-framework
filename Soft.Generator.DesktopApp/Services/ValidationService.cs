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
