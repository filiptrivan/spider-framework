namespace Soft.Generator.DesktopApp.Pages
{
    partial class HomePage
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
            label1 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            label1.Location = new Point(22, 0);
            label1.Name = "label1";
            label1.Padding = new Padding(2);
            label1.Size = new Size(364, 36);
            label1.TabIndex = 0;
            label1.Text = "Dobrodošli na Soft Generator!";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // HomePage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.AppWorkspace;
            Controls.Add(label1);
            Name = "HomePage";
            Size = new Size(409, 36);
            Load += HomePage_Load;
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
    }
}
