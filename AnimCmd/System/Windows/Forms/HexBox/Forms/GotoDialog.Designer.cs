namespace Be.Windows.Forms
{
    partial class GotoDialog
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnDecimal = new System.Windows.Forms.RadioButton();
            this.btnHex = new System.Windows.Forms.RadioButton();
            this.radioEnd = new System.Windows.Forms.RadioButton();
            this.radioHere = new System.Windows.Forms.RadioButton();
            this.radioBegin = new System.Windows.Forms.RadioButton();
            this.btnOkay = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(68, 25);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(127, 20);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Offset:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Controls.Add(this.radioEnd);
            this.groupBox1.Controls.Add(this.radioHere);
            this.groupBox1.Controls.Add(this.radioBegin);
            this.groupBox1.Location = new System.Drawing.Point(12, 55);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(183, 98);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Offset Relative To";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnDecimal);
            this.groupBox2.Controls.Add(this.btnHex);
            this.groupBox2.Location = new System.Drawing.Point(95, 20);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(73, 67);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            // 
            // btnDecimal
            // 
            this.btnDecimal.AutoSize = true;
            this.btnDecimal.Location = new System.Drawing.Point(6, 37);
            this.btnDecimal.Name = "btnDecimal";
            this.btnDecimal.Size = new System.Drawing.Size(63, 17);
            this.btnDecimal.TabIndex = 1;
            this.btnDecimal.Text = "Decimal";
            this.btnDecimal.UseVisualStyleBackColor = true;
            // 
            // btnHex
            // 
            this.btnHex.AutoSize = true;
            this.btnHex.Checked = true;
            this.btnHex.Location = new System.Drawing.Point(6, 13);
            this.btnHex.Name = "btnHex";
            this.btnHex.Size = new System.Drawing.Size(44, 17);
            this.btnHex.TabIndex = 0;
            this.btnHex.TabStop = true;
            this.btnHex.Text = "Hex";
            this.btnHex.UseVisualStyleBackColor = true;
            // 
            // radioEnd
            // 
            this.radioEnd.AutoSize = true;
            this.radioEnd.Location = new System.Drawing.Point(7, 66);
            this.radioEnd.Name = "radioEnd";
            this.radioEnd.Size = new System.Drawing.Size(44, 17);
            this.radioEnd.TabIndex = 2;
            this.radioEnd.Text = "End";
            this.radioEnd.UseVisualStyleBackColor = true;
            // 
            // radioHere
            // 
            this.radioHere.AutoSize = true;
            this.radioHere.Location = new System.Drawing.Point(7, 43);
            this.radioHere.Name = "radioHere";
            this.radioHere.Size = new System.Drawing.Size(48, 17);
            this.radioHere.TabIndex = 1;
            this.radioHere.Text = "Here";
            this.radioHere.UseVisualStyleBackColor = true;
            // 
            // radioBegin
            // 
            this.radioBegin.AutoSize = true;
            this.radioBegin.Checked = true;
            this.radioBegin.Location = new System.Drawing.Point(7, 20);
            this.radioBegin.Name = "radioBegin";
            this.radioBegin.Size = new System.Drawing.Size(72, 17);
            this.radioBegin.TabIndex = 0;
            this.radioBegin.TabStop = true;
            this.radioBegin.Text = "Beginning";
            this.radioBegin.UseVisualStyleBackColor = true;
            // 
            // btnOkay
            // 
            this.btnOkay.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOkay.Location = new System.Drawing.Point(105, 177);
            this.btnOkay.Name = "btnOkay";
            this.btnOkay.Size = new System.Drawing.Size(90, 24);
            this.btnOkay.TabIndex = 0;
            this.btnOkay.Text = "Go";
            this.btnOkay.UseVisualStyleBackColor = true;
            this.btnOkay.Click += new System.EventHandler(this.btnOkay_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(10, 177);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(89, 24);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // GotoDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(205, 213);
            this.ControlBox = false;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOkay);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GotoDialog";
            this.ShowIcon = false;
            this.Text = "Goto";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnOkay;
        private System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.RadioButton radioEnd;
        public System.Windows.Forms.RadioButton radioHere;
        public System.Windows.Forms.RadioButton radioBegin;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton btnDecimal;
        private System.Windows.Forms.RadioButton btnHex;
    }
}