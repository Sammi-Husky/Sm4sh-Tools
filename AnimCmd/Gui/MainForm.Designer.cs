namespace Sm4shCommand
{
    partial class AcmdMain
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AcmdMain));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aCMDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mSCSBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mTABLEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.workspaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.fighterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.workspaceToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.dumpAsTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.eventLibraryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.parseAnimationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.FileTree = new System.Windows.Forms.TreeView();
            this.pnlRight = new System.Windows.Forms.Panel();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.itsCodeBox1 = new Sm4shCommand.ITSCodeBox();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.pnlRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.Control;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(844, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator2,
            this.dumpAsTextToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aCMDToolStripMenuItem,
            this.mSCSBToolStripMenuItem,
            this.mTABLEToolStripMenuItem,
            this.toolStripSeparator4,
            this.workspaceToolStripMenuItem});
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.newToolStripMenuItem.Text = "New";
            // 
            // aCMDToolStripMenuItem
            // 
            this.aCMDToolStripMenuItem.Name = "aCMDToolStripMenuItem";
            this.aCMDToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.aCMDToolStripMenuItem.Text = "ACMD";
            // 
            // mSCSBToolStripMenuItem
            // 
            this.mSCSBToolStripMenuItem.Name = "mSCSBToolStripMenuItem";
            this.mSCSBToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.mSCSBToolStripMenuItem.Text = "MSCSB";
            // 
            // mTABLEToolStripMenuItem
            // 
            this.mTABLEToolStripMenuItem.Name = "mTABLEToolStripMenuItem";
            this.mTABLEToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.mTABLEToolStripMenuItem.Text = "MTABLE";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(208, 6);
            // 
            // workspaceToolStripMenuItem
            // 
            this.workspaceToolStripMenuItem.Name = "workspaceToolStripMenuItem";
            this.workspaceToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.workspaceToolStripMenuItem.Text = "Project From Existing Files";
            this.workspaceToolStripMenuItem.Click += new System.EventHandler(this.newWorkspaceToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem1,
            this.fighterToolStripMenuItem,
            this.toolStripSeparator3,
            this.workspaceToolStripMenuItem1});
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.openToolStripMenuItem.Text = "Open";
            // 
            // fileToolStripMenuItem1
            // 
            this.fileToolStripMenuItem1.Name = "fileToolStripMenuItem1";
            this.fileToolStripMenuItem1.Size = new System.Drawing.Size(138, 22);
            this.fileToolStripMenuItem1.Text = "File..";
            this.fileToolStripMenuItem1.Click += new System.EventHandler(this.fileToolStripMenuItem1_Click);
            // 
            // fighterToolStripMenuItem
            // 
            this.fighterToolStripMenuItem.Name = "fighterToolStripMenuItem";
            this.fighterToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.fighterToolStripMenuItem.Text = "Fighter..";
            this.fighterToolStripMenuItem.Click += new System.EventHandler(this.fighterToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(135, 6);
            // 
            // workspaceToolStripMenuItem1
            // 
            this.workspaceToolStripMenuItem1.Name = "workspaceToolStripMenuItem1";
            this.workspaceToolStripMenuItem1.Size = new System.Drawing.Size(138, 22);
            this.workspaceToolStripMenuItem1.Text = "Workspace..";
            this.workspaceToolStripMenuItem1.Click += new System.EventHandler(this.workspaceToolStripMenuItem1_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(187, 6);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.saveAsToolStripMenuItem.Text = "Save as..";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(187, 6);
            // 
            // dumpAsTextToolStripMenuItem
            // 
            this.dumpAsTextToolStripMenuItem.Name = "dumpAsTextToolStripMenuItem";
            this.dumpAsTextToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.dumpAsTextToolStripMenuItem.Text = "Dump as Text";
            this.dumpAsTextToolStripMenuItem.Click += new System.EventHandler(this.dumpAsTextToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.eventLibraryToolStripMenuItem,
            this.parseAnimationsToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // eventLibraryToolStripMenuItem
            // 
            this.eventLibraryToolStripMenuItem.Name = "eventLibraryToolStripMenuItem";
            this.eventLibraryToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.eventLibraryToolStripMenuItem.Text = "Event Library";
            this.eventLibraryToolStripMenuItem.Click += new System.EventHandler(this.eventLibraryToolStripMenuItem_Click);
            // 
            // parseAnimationsToolStripMenuItem
            // 
            this.parseAnimationsToolStripMenuItem.Name = "parseAnimationsToolStripMenuItem";
            this.parseAnimationsToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.parseAnimationsToolStripMenuItem.Text = "Parse Animations";
            this.parseAnimationsToolStripMenuItem.Click += new System.EventHandler(this.parseAnimationsToolStripMenuItem_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.FileTree);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(186, 604);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Workspace";
            // 
            // FileTree
            // 
            this.FileTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FileTree.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FileTree.HideSelection = false;
            this.FileTree.Location = new System.Drawing.Point(3, 16);
            this.FileTree.Name = "FileTree";
            this.FileTree.ShowNodeToolTips = true;
            this.FileTree.Size = new System.Drawing.Size(180, 585);
            this.FileTree.TabIndex = 0;
            this.FileTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.FileTree_MouseDoubleClick);
            // 
            // pnlRight
            // 
            this.pnlRight.Controls.Add(this.groupBox1);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlRight.Location = new System.Drawing.Point(658, 24);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Size = new System.Drawing.Size(186, 604);
            this.pnlRight.TabIndex = 10;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Right;
            this.splitter1.Location = new System.Drawing.Point(655, 24);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 604);
            this.splitter1.TabIndex = 11;
            this.splitter1.TabStop = false;
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.Color.White;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.richTextBox1.Enabled = false;
            this.richTextBox1.ForeColor = System.Drawing.Color.Lime;
            this.richTextBox1.Location = new System.Drawing.Point(0, 514);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(655, 114);
            this.richTextBox1.TabIndex = 13;
            this.richTextBox1.Text = "";
            // 
            // tabControl1
            // 
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabControl1.ItemSize = new System.Drawing.Size(150, 18);
            this.tabControl1.Location = new System.Drawing.Point(0, 24);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(655, 490);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl1.TabIndex = 9;
            this.tabControl1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.tabControl1_DrawItem);
            this.tabControl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.tabControl1_MouseClick);
            // 
            // itsCodeBox1
            // 
            this.itsCodeBox1.AutoScroll = true;
            this.itsCodeBox1.CharHeight = 17;
            this.itsCodeBox1.CharWidth = 8;
            this.itsCodeBox1.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.itsCodeBox1.Font = new System.Drawing.Font("Courier New", 9.75F);
            this.itsCodeBox1.IndentWidth = 32F;
            this.itsCodeBox1.Location = new System.Drawing.Point(0, 0);
            this.itsCodeBox1.Name = "itsCodeBox1";
            this.itsCodeBox1.Size = new System.Drawing.Size(150, 150);
            this.itsCodeBox1.TabIndex = 0;
            // 
            // AcmdMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(844, 628);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.pnlRight);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "AcmdMain";
            this.Text = "AnimCmd";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ACMDMain_FormClosing);
            this.Load += new System.EventHandler(this.ACMDMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void CmdListTree_BeforeSelect(object sender, System.Windows.Forms.TreeViewCancelEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem eventLibraryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dumpAsTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem workspaceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem workspaceToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem parseAnimationsToolStripMenuItem;
        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TreeView FileTree;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem fighterToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem aCMDToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mSCSBToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mTABLEToolStripMenuItem;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TabControl tabControl1;
        private ITSCodeBox itsCodeBox1;
    }
}

