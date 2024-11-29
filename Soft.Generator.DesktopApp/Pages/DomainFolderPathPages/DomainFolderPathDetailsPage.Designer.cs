namespace Soft.Generator.DesktopApp.Pages.DomainFolderPathPages
{
    partial class DomainFolderPathDetailsPage
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
            tb_Path = new SoftTextbox();
            SuspendLayout();
            // 
            // tb_Path
            // 
            tb_Path.LabelValue = "Putanja domenskog foldera";
            tb_Path.Location = new Point(3, 3);
            tb_Path.Name = "tb_Path";
            tb_Path.Size = new Size(238, 63);
            tb_Path.TabIndex = 0;
            tb_Path.TextBoxValue = "";
            // 
            // DomainFolderPathDetailsPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tb_Path);
            Name = "DomainFolderPathDetailsPage";
            Size = new Size(520, 251);
            ResumeLayout(false);
        }

        #endregion

        private SoftTextbox tb_Path;
    }
}
