namespace Soft.Generator.DesktopApp.Pages.CompanyPages
{
    partial class CompanyDetailsPage
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
            tb_Email = new SoftTextbox();
            tb_Password = new SoftTextbox();
            btn_Save = new Button();
            btn_Return = new Button();
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
            // tb_Email
            // 
            tb_Email.LabelValue = "Email";
            tb_Email.Location = new Point(247, 3);
            tb_Email.Name = "tb_Email";
            tb_Email.Size = new Size(238, 63);
            tb_Email.TabIndex = 1;
            tb_Email.TextBoxValue = "";
            // 
            // tb_Password
            // 
            tb_Password.LabelValue = "Lozinka";
            tb_Password.Location = new Point(3, 72);
            tb_Password.Name = "tb_Password";
            tb_Password.Size = new Size(238, 63);
            tb_Password.TabIndex = 2;
            tb_Password.TextBoxValue = "";
            // 
            // btn_Save
            // 
            btn_Save.Location = new Point(16, 141);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(75, 23);
            btn_Save.TabIndex = 3;
            btn_Save.Text = "Sačuvaj";
            btn_Save.UseVisualStyleBackColor = true;
            btn_Save.Click += btn_Save_Click;
            // 
            // btn_Return
            // 
            btn_Return.Location = new Point(107, 141);
            btn_Return.Name = "btn_Return";
            btn_Return.Size = new Size(75, 23);
            btn_Return.TabIndex = 4;
            btn_Return.Text = "Vrati se";
            btn_Return.UseVisualStyleBackColor = true;
            btn_Return.Click += btn_Return_Click;
            // 
            // CompanyDetailsPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btn_Return);
            Controls.Add(btn_Save);
            Controls.Add(tb_Password);
            Controls.Add(tb_Email);
            Controls.Add(tb_Name);
            Name = "CompanyDetailsPage";
            Size = new Size(584, 311);
            ResumeLayout(false);
        }

        #endregion

        private SoftTextbox tb_Name;
        private SoftTextbox tb_Email;
        private SoftTextbox tb_Password;
        private Button btn_Save;
        private Button btn_Return;
    }
}
