namespace Soft.Generator.DesktopApp.Pages
{
    partial class ApplicationListPage
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
            softTextbox1 = new SoftTextbox();
            SuspendLayout();
            // 
            // softTextbox1
            // 
            softTextbox1.LabelValue = "label1e";
            softTextbox1.Location = new Point(0, 0);
            softTextbox1.Name = "softTextbox1";
            softTextbox1.Size = new Size(238, 63);
            softTextbox1.TabIndex = 1;
            softTextbox1.TextBoxValue = "";
            softTextbox1.Load += softTextbox1_Load;
            // 
            // ApplicationPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(softTextbox1);
            Name = "ApplicationPage";
            Size = new Size(666, 150);
            ResumeLayout(false);
        }

        #endregion

        private SoftTextbox softTextbox1;
    }
}
