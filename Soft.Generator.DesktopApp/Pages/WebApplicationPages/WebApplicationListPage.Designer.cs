namespace Soft.Generator.DesktopApp.Pages
{
    partial class WebApplicationListPage
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
            softDataGridView1 = new Controls.SoftDataGridView();
            SuspendLayout();
            // 
            // softDataGridView1
            // 
            softDataGridView1.Dock = DockStyle.Fill;
            softDataGridView1.Location = new Point(0, 0);
            softDataGridView1.Name = "softDataGridView1";
            softDataGridView1.Size = new Size(666, 313);
            softDataGridView1.TabIndex = 0;
            // 
            // ApplicationListPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(softDataGridView1);
            Name = "WebApplicationListPage";
            Size = new Size(666, 313);
            ResumeLayout(false);
        }

        #endregion

        private Controls.SoftDataGridView softDataGridView1;
    }
}
