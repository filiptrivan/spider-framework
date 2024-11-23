namespace Soft.Generator.DesktopApp.Controls
{
    partial class SoftDataGridView
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
            dataGridView1 = new DataGridView();
            Details = new DataGridViewButtonColumn();
            Delete = new DataGridViewButtonColumn();
            btn_AddNew = new Button();
            panel1 = new Panel();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Details, Delete });
            dataGridView1.Location = new Point(0, 0);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.Size = new Size(555, 276);
            dataGridView1.TabIndex = 0;
            // 
            // Details
            // 
            Details.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Details.FillWeight = 101.522842F;
            Details.HeaderText = "Detalji";
            Details.Name = "Details";
            Details.ReadOnly = true;
            Details.Text = "Detalji";
            Details.ToolTipText = "Detalji";
            Details.UseColumnTextForButtonValue = true;
            // 
            // Delete
            // 
            Delete.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Delete.FillWeight = 98.4771652F;
            Delete.HeaderText = "Brisanje";
            Delete.Name = "Delete";
            Delete.ReadOnly = true;
            Delete.Text = "Brisanje";
            Delete.ToolTipText = "Brisanje";
            Delete.UseColumnTextForButtonValue = true;
            // 
            // btn_AddNew
            // 
            btn_AddNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_AddNew.Location = new Point(477, 280);
            btn_AddNew.Name = "btn_AddNew";
            btn_AddNew.Size = new Size(75, 23);
            btn_AddNew.TabIndex = 1;
            btn_AddNew.Text = "Dodaj";
            btn_AddNew.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = SystemColors.ActiveBorder;
            panel1.Controls.Add(btn_AddNew);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(555, 308);
            panel1.TabIndex = 2;
            // 
            // SoftDataGridView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(dataGridView1);
            Controls.Add(panel1);
            Name = "SoftDataGridView";
            Size = new Size(555, 331);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dataGridView1;
        private Button btn_AddNew;
        private Panel panel1;
        private DataGridViewButtonColumn Details;
        private DataGridViewButtonColumn Delete;
    }
}
