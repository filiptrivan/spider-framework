namespace Soft.Generator.DesktopApp.Pages.FrameworkPages
{
    partial class FrameworkDetailsPage
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
            softTextbox2 = new SoftTextbox();
            softTextbox3 = new SoftTextbox();
            button1 = new Button();
            SuspendLayout();
            // 
            // softTextbox1
            // 
            softTextbox1.LabelValue = "label1";
            softTextbox1.Location = new Point(3, 3);
            softTextbox1.Name = "softTextbox1";
            softTextbox1.Size = new Size(238, 63);
            softTextbox1.TabIndex = 0;
            softTextbox1.TextBoxValue = "";
            // 
            // softTextbox2
            // 
            softTextbox2.LabelValue = "label1";
            softTextbox2.Location = new Point(247, 3);
            softTextbox2.Name = "softTextbox2";
            softTextbox2.Size = new Size(238, 63);
            softTextbox2.TabIndex = 1;
            softTextbox2.TextBoxValue = "";
            // 
            // softTextbox3
            // 
            softTextbox3.LabelValue = "label1";
            softTextbox3.Location = new Point(3, 72);
            softTextbox3.Name = "softTextbox3";
            softTextbox3.Size = new Size(238, 63);
            softTextbox3.TabIndex = 2;
            softTextbox3.TextBoxValue = "";
            // 
            // button1
            // 
            button1.Location = new Point(17, 152);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 3;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // FrameworkDetailsPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(button1);
            Controls.Add(softTextbox3);
            Controls.Add(softTextbox2);
            Controls.Add(softTextbox1);
            Name = "FrameworkDetailsPage";
            Size = new Size(574, 199);
            ResumeLayout(false);
        }

        #endregion

        private SoftTextbox softTextbox1;
        private SoftTextbox softTextbox2;
        private SoftTextbox softTextbox3;
        private Button button1;
    }
}
