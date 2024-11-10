namespace Soft.Generator.DesktopApp.Pages
{
    partial class PermissionListPage
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
            components = new System.ComponentModel.Container();
            permissionBindingSource = new BindingSource(components);
            softDataGridView1 = new Controls.SoftDataGridView();
            ((System.ComponentModel.ISupportInitialize)permissionBindingSource).BeginInit();
            SuspendLayout();
            // 
            // permissionBindingSource
            // 
            permissionBindingSource.DataSource = typeof(Entities.Permission);
            // 
            // softDataGridView1
            // 
            softDataGridView1.Dock = DockStyle.Fill;
            softDataGridView1.Location = new Point(0, 0);
            softDataGridView1.Name = "softDataGridView1";
            softDataGridView1.Size = new Size(600, 200);
            softDataGridView1.TabIndex = 0;
            // 
            // PermissionPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(softDataGridView1);
            Name = "PermissionPage";
            Size = new Size(600, 200);
            ((System.ComponentModel.ISupportInitialize)permissionBindingSource).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private BindingSource permissionBindingSource;
        private Controls.SoftDataGridView softDataGridView1;
    }
}
