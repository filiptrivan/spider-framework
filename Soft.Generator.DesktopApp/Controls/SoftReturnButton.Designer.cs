namespace Soft.Generator.DesktopApp.Controls
{
    partial class SoftReturnButton
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
            btn_Return = new Button();
            SuspendLayout();
            // 
            // btn_Return
            // 
            btn_Return.Location = new Point(3, 3);
            btn_Return.Name = "btn_Return";
            btn_Return.Size = new Size(75, 23);
            btn_Return.TabIndex = 0;
            btn_Return.Text = "Vrati se";
            btn_Return.UseVisualStyleBackColor = true;
            btn_Return.Click += btn_Return_Click;
            // 
            // SoftReturnButton
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btn_Return);
            Name = "SoftReturnButton";
            Size = new Size(81, 30);
            ResumeLayout(false);
        }

        #endregion

        private Button btn_Return;
    }
}
