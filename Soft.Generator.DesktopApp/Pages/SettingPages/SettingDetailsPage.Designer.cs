namespace Soft.Generator.DesktopApp.Pages.SettingPages
{
    partial class SettingDetailsPage
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            chb_HasGoogleAuth = new Controls.SoftCheckbox();
            tb_PrimaryColor = new SoftTextbox();
            chb_HasLatinTranslate = new Controls.SoftCheckbox();
            chb_HasDarkMode = new Controls.SoftCheckbox();
            chb_HasNotifications = new Controls.SoftCheckbox();
            cb_Framework = new Controls.SoftCombobox();
            tb_Name = new SoftTextbox();
            btn_Save = new Button();
            btn_Return = new Button();
            SuspendLayout();
            // 
            // chb_HasGoogleAuth
            // 
            chb_HasGoogleAuth.LabelValue = "Ima Google prijavu";
            chb_HasGoogleAuth.Location = new Point(249, 5);
            chb_HasGoogleAuth.Name = "chb_HasGoogleAuth";
            chb_HasGoogleAuth.Size = new Size(238, 63);
            chb_HasGoogleAuth.TabIndex = 0;
            chb_HasGoogleAuth.Value = false;
            // 
            // tb_PrimaryColor
            // 
            tb_PrimaryColor.LabelValue = "Primarna boja";
            tb_PrimaryColor.Location = new Point(5, 74);
            tb_PrimaryColor.Name = "tb_PrimaryColor";
            tb_PrimaryColor.Size = new Size(238, 63);
            tb_PrimaryColor.TabIndex = 1;
            tb_PrimaryColor.TextBoxValue = "";
            // 
            // chb_HasLatinTranslate
            // 
            chb_HasLatinTranslate.LabelValue = "Ima prevod na latinicu";
            chb_HasLatinTranslate.Location = new Point(249, 74);
            chb_HasLatinTranslate.Name = "chb_HasLatinTranslate";
            chb_HasLatinTranslate.Size = new Size(238, 63);
            chb_HasLatinTranslate.TabIndex = 2;
            chb_HasLatinTranslate.Value = false;
            // 
            // chb_HasDarkMode
            // 
            chb_HasDarkMode.LabelValue = "Ima tamni režim";
            chb_HasDarkMode.Location = new Point(5, 143);
            chb_HasDarkMode.Name = "chb_HasDarkMode";
            chb_HasDarkMode.Size = new Size(238, 63);
            chb_HasDarkMode.TabIndex = 3;
            chb_HasDarkMode.Value = false;
            // 
            // chb_HasNotifications
            // 
            chb_HasNotifications.LabelValue = "Ima notifikacije";
            chb_HasNotifications.Location = new Point(249, 143);
            chb_HasNotifications.Name = "chb_HasNotifications";
            chb_HasNotifications.Size = new Size(238, 63);
            chb_HasNotifications.TabIndex = 4;
            chb_HasNotifications.Value = false;
            // 
            // cb_Framework
            // 
            cb_Framework.DisplayMember = "Id";
            cb_Framework.LabelValue = "Okvir";
            cb_Framework.Location = new Point(5, 212);
            cb_Framework.Name = "cb_Framework";
            cb_Framework.Size = new Size(238, 63);
            cb_Framework.TabIndex = 5;
            // 
            // tb_Name
            // 
            tb_Name.LabelValue = "Naziv";
            tb_Name.Location = new Point(5, 5);
            tb_Name.Name = "tb_Name";
            tb_Name.Size = new Size(238, 63);
            tb_Name.TabIndex = 6;
            tb_Name.TextBoxValue = "";
            // 
            // btn_Save
            // 
            btn_Save.Location = new Point(17, 285);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(75, 23);
            btn_Save.TabIndex = 7;
            btn_Save.Text = "Sačuvaj";
            btn_Save.UseVisualStyleBackColor = true;
            btn_Save.Click += btn_Save_Click;
            // 
            // btn_Return
            // 
            btn_Return.Location = new Point(109, 285);
            btn_Return.Name = "btn_Return";
            btn_Return.Size = new Size(75, 23);
            btn_Return.TabIndex = 8;
            btn_Return.Text = "Vrati se";
            btn_Return.UseVisualStyleBackColor = true;
            // 
            // SettingDetailsPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btn_Return);
            Controls.Add(btn_Save);
            Controls.Add(tb_Name);
            Controls.Add(cb_Framework);
            Controls.Add(chb_HasNotifications);
            Controls.Add(chb_HasDarkMode);
            Controls.Add(chb_HasLatinTranslate);
            Controls.Add(tb_PrimaryColor);
            Controls.Add(chb_HasGoogleAuth);
            Name = "SettingDetailsPage";
            Size = new Size(596, 363);
            ResumeLayout(false);
        }

        #endregion

        private Controls.SoftCheckbox chb_HasGoogleAuth;
        private SoftTextbox tb_PrimaryColor;
        private Controls.SoftCheckbox chb_HasLatinTranslate;
        private Controls.SoftCheckbox chb_HasDarkMode;
        private Controls.SoftCheckbox chb_HasNotifications;
        private Controls.SoftCombobox cb_Framework;
        private SoftTextbox tb_Name;
        private Button btn_Save;
        private Button btn_Return;
    }
}
