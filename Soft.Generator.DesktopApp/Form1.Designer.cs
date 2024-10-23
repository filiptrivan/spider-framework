namespace Soft.Generator.DesktopApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            softTextbox1 = new SoftTextbox();
            menuStrip1 = new MenuStrip();
            homeToolStripMenuItem = new ToolStripMenuItem();
            applicationToolStripMenuItem = new ToolStripMenuItem();
            companyToolStripMenuItem = new ToolStripMenuItem();
            settingToolStripMenuItem = new ToolStripMenuItem();
            codebookToolStripMenuItem = new ToolStripMenuItem();
            frameworkToolStripMenuItem = new ToolStripMenuItem();
            permissionToolStripMenuItem = new ToolStripMenuItem();
            pathToDomainFolderToolStripMenuItem = new ToolStripMenuItem();
            pnl_Main = new Panel();
            menuStrip1.SuspendLayout();
            pnl_Main.SuspendLayout();
            SuspendLayout();
            // 
            // softTextbox1
            // 
            softTextbox1.LabelValue = "label1e";
            softTextbox1.Location = new Point(3, 3);
            softTextbox1.Name = "softTextbox1";
            softTextbox1.Size = new Size(238, 63);
            softTextbox1.TabIndex = 0;
            softTextbox1.TextBoxValue = "";
            softTextbox1.Load += softTextbox1_Load;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { homeToolStripMenuItem, applicationToolStripMenuItem, companyToolStripMenuItem, settingToolStripMenuItem, codebookToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // homeToolStripMenuItem
            // 
            homeToolStripMenuItem.Name = "homeToolStripMenuItem";
            homeToolStripMenuItem.Size = new Size(62, 20);
            homeToolStripMenuItem.Text = "Početna";
            homeToolStripMenuItem.Click += početnaToolStripMenuItem_Click;
            // 
            // applicationToolStripMenuItem
            // 
            applicationToolStripMenuItem.Name = "applicationToolStripMenuItem";
            applicationToolStripMenuItem.Size = new Size(70, 20);
            applicationToolStripMenuItem.Text = "Aplikacija";
            // 
            // companyToolStripMenuItem
            // 
            companyToolStripMenuItem.Name = "companyToolStripMenuItem";
            companyToolStripMenuItem.Size = new Size(76, 20);
            companyToolStripMenuItem.Text = "Kompanija";
            // 
            // settingToolStripMenuItem
            // 
            settingToolStripMenuItem.Name = "settingToolStripMenuItem";
            settingToolStripMenuItem.Size = new Size(85, 20);
            settingToolStripMenuItem.Text = "Podešavanje";
            // 
            // codebookToolStripMenuItem
            // 
            codebookToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { frameworkToolStripMenuItem, permissionToolStripMenuItem, pathToDomainFolderToolStripMenuItem });
            codebookToolStripMenuItem.Name = "codebookToolStripMenuItem";
            codebookToolStripMenuItem.Size = new Size(61, 20);
            codebookToolStripMenuItem.Text = "Šifarnici";
            // 
            // frameworkToolStripMenuItem
            // 
            frameworkToolStripMenuItem.Name = "frameworkToolStripMenuItem";
            frameworkToolStripMenuItem.Size = new Size(237, 22);
            frameworkToolStripMenuItem.Text = "Okvir";
            // 
            // permissionToolStripMenuItem
            // 
            permissionToolStripMenuItem.Name = "permissionToolStripMenuItem";
            permissionToolStripMenuItem.Size = new Size(237, 22);
            permissionToolStripMenuItem.Text = "Permisija";
            // 
            // pathToDomainFolderToolStripMenuItem
            // 
            pathToDomainFolderToolStripMenuItem.Name = "pathToDomainFolderToolStripMenuItem";
            pathToDomainFolderToolStripMenuItem.Size = new Size(237, 22);
            pathToDomainFolderToolStripMenuItem.Text = "Putanja do domenskog foldera";
            // 
            // pnl_Main
            // 
            pnl_Main.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnl_Main.BackColor = SystemColors.ActiveCaption;
            pnl_Main.Controls.Add(softTextbox1);
            pnl_Main.Location = new Point(12, 27);
            pnl_Main.Name = "pnl_Main";
            pnl_Main.Size = new Size(776, 411);
            pnl_Main.TabIndex = 2;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pnl_Main);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            pnl_Main.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SoftTextbox softTextbox1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem applicationToolStripMenuItem;
        private ToolStripMenuItem companyToolStripMenuItem;
        private ToolStripMenuItem settingToolStripMenuItem;
        private ToolStripMenuItem codebookToolStripMenuItem;
        private ToolStripMenuItem frameworkToolStripMenuItem;
        private ToolStripMenuItem permissionToolStripMenuItem;
        private ToolStripMenuItem pathToDomainFolderToolStripMenuItem;
        private ToolStripMenuItem homeToolStripMenuItem;
        private Panel pnl_Main;
    }
}
