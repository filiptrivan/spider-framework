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
            tb_Name = new SoftTextbox();
            tb_Code = new SoftTextbox();
            btn_Save = new Button();
            btn_Return = new Button();
            SuspendLayout();
            // 
            // tb_Name
            // 
            tb_Name.LabelValue = "Name";
            tb_Name.Location = new Point(3, 3);
            tb_Name.Name = "tb_Name";
            tb_Name.Size = new Size(238, 63);
            tb_Name.TabIndex = 0;
            tb_Name.TextBoxValue = "";
            // 
            // tb_Code
            // 
            tb_Code.LabelValue = "Code";
            tb_Code.Location = new Point(247, 3);
            tb_Code.Name = "tb_Code";
            tb_Code.Size = new Size(238, 63);
            tb_Code.TabIndex = 1;
            tb_Code.TextBoxValue = "";
            // 
            // btn_Save
            // 
            btn_Save.Location = new Point(16, 72);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(75, 23);
            btn_Save.TabIndex = 3;
            btn_Save.Text = "Sačuvaj";
            btn_Save.UseVisualStyleBackColor = true;
            btn_Save.Click += btn_Save_Click;
            // 
            // btn_Return
            // 
            btn_Return.Location = new Point(106, 72);
            btn_Return.Name = "btn_Return";
            btn_Return.Size = new Size(75, 23);
            btn_Return.TabIndex = 4;
            btn_Return.Text = "Vrati se";
            btn_Return.UseVisualStyleBackColor = true;
            btn_Return.Click += btn_Return_Click;
            // 
            // FrameworkDetailsPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btn_Return);
            Controls.Add(btn_Save);
            Controls.Add(tb_Code);
            Controls.Add(tb_Name);
            Name = "FrameworkDetailsPage";
            Size = new Size(574, 199);
            ResumeLayout(false);
        }

        #endregion

        private SoftTextbox tb_Name;
        private SoftTextbox tb_Code;
        private Button btn_Save;
        private Button btn_Return;
    }
}
