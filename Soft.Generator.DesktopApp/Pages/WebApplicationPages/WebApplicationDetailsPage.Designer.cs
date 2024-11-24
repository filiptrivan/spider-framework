namespace Soft.Generator.DesktopApp.Pages
{
    partial class WebApplicationDetailsPage
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
            tb_Name = new SoftTextbox();
            cb_Setting = new Controls.SoftCombobox();
            cb_Company = new Controls.SoftCombobox();
            SuspendLayout();
            // 
            // tb_Name
            // 
            tb_Name.LabelValue = "Naziv";
            tb_Name.Location = new Point(3, 3);
            tb_Name.Name = "tb_Name";
            tb_Name.Size = new Size(238, 63);
            tb_Name.TabIndex = 0;
            tb_Name.TextBoxValue = "";
            // 
            // cb_Setting
            // 
            cb_Setting.LabelValue = "Podešavanje";
            cb_Setting.Location = new Point(247, 3);
            cb_Setting.Name = "cb_Setting";
            cb_Setting.Size = new Size(238, 63);
            cb_Setting.TabIndex = 1;
            // 
            // cb_Company
            // 
            cb_Company.LabelValue = "Kompanija";
            cb_Company.Location = new Point(3, 72);
            cb_Company.Name = "cb_Company";
            cb_Company.Size = new Size(238, 63);
            cb_Company.TabIndex = 2;
            // 
            // WebApplicationDetailsPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(cb_Company);
            Controls.Add(cb_Setting);
            Controls.Add(tb_Name);
            Name = "WebApplicationDetailsPage";
            Size = new Size(609, 276);
            ResumeLayout(false);
        }

        #endregion

        private SoftTextbox tb_Name;
        private Controls.SoftCombobox cb_Setting;
        private Controls.SoftCombobox cb_Company;
    }
}
