namespace Sm4shCommand
{
    partial class HexView
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteWriteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteInsertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.gotoAdressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectBlockToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertBytesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.txtOffset = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusOffset = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSelLength = new System.Windows.Forms.ToolStripStatusLabel();
            this.hexBox = new Be.Windows.Forms.HexBox();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(685, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.pasteWriteToolStripMenuItem,
            this.pasteInsertToolStripMenuItem,
            this.toolStripSeparator1,
            this.gotoAdressToolStripMenuItem,
            this.selectBlockToolStripMenuItem,
            this.insertBytesToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            // 
            // pasteWriteToolStripMenuItem
            // 
            this.pasteWriteToolStripMenuItem.Name = "pasteWriteToolStripMenuItem";
            this.pasteWriteToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.pasteWriteToolStripMenuItem.Text = "Paste Write";
            // 
            // pasteInsertToolStripMenuItem
            // 
            this.pasteInsertToolStripMenuItem.Name = "pasteInsertToolStripMenuItem";
            this.pasteInsertToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.pasteInsertToolStripMenuItem.Text = "Paste Insert";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(141, 6);
            // 
            // gotoAdressToolStripMenuItem
            // 
            this.gotoAdressToolStripMenuItem.Name = "gotoAdressToolStripMenuItem";
            this.gotoAdressToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.gotoAdressToolStripMenuItem.Text = "Goto Adress..";
            this.gotoAdressToolStripMenuItem.Click += new System.EventHandler(this.gotoAdressToolStripMenuItem_Click);
            // 
            // selectBlockToolStripMenuItem
            // 
            this.selectBlockToolStripMenuItem.Name = "selectBlockToolStripMenuItem";
            this.selectBlockToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.selectBlockToolStripMenuItem.Text = "Select Block..";
            // 
            // insertBytesToolStripMenuItem
            // 
            this.insertBytesToolStripMenuItem.Name = "insertBytesToolStripMenuItem";
            this.insertBytesToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.insertBytesToolStripMenuItem.Text = "Insert bytes..";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.txtOffset,
            this.statusOffset,
            this.toolStripStatusLabel1,
            this.statusSelLength});
            this.statusStrip1.Location = new System.Drawing.Point(0, 448);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(685, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // txtOffset
            // 
            this.txtOffset.Name = "txtOffset";
            this.txtOffset.Size = new System.Drawing.Size(39, 17);
            this.txtOffset.Text = "Offset";
            // 
            // statusOffset
            // 
            this.statusOffset.Name = "statusOffset";
            this.statusOffset.Size = new System.Drawing.Size(24, 17);
            this.statusOffset.Text = "0x0";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(44, 17);
            this.toolStripStatusLabel1.Text = "Length";
            // 
            // statusSelLength
            // 
            this.statusSelLength.Name = "statusSelLength";
            this.statusSelLength.Size = new System.Drawing.Size(24, 17);
            this.statusSelLength.Text = "0x0";
            // 
            // hexBox
            // 
            this.hexBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hexBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.hexBox.Font = new System.Drawing.Font("Courier New", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexBox.ForeColor = System.Drawing.Color.Black;
            this.hexBox.LineInfoForeColor = System.Drawing.Color.Empty;
            this.hexBox.LineInfoVisible = true;
            this.hexBox.Location = new System.Drawing.Point(12, 28);
            this.hexBox.Name = "hexBox";
            this.hexBox.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.hexBox.Size = new System.Drawing.Size(661, 410);
            this.hexBox.StringViewVisible = true;
            this.hexBox.TabIndex = 0;
            this.hexBox.UseFixedBytesPerLine = true;
            this.hexBox.VScrollBarVisible = true;
            this.hexBox.SelectionLengthChanged += new System.EventHandler(this.hexBox_SelectionLengthChanged);
            this.hexBox.CurrentPositionInLineChanged += new System.EventHandler(this.hexBox_CurrentPositionInLineChanged);
            // 
            // HexView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(685, 470);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.hexBox);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "HexView";
            this.Text = "HexView - ";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gotoAdressToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectBlockToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteWriteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteInsertToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem insertBytesToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel txtOffset;
        private System.Windows.Forms.ToolStripStatusLabel statusOffset;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel statusSelLength;
        public Be.Windows.Forms.HexBox hexBox;
    }
}