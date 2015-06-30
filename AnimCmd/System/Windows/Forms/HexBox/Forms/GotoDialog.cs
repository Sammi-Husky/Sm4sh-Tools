using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Be.Windows.Forms
{
    public partial class GotoDialog : Form
    {
        public long offset;
        bool errorstatus = false;

        public HexBox HexEditor { get { return _hexEditor; } set { _hexEditor = value; } }
        private HexBox _hexEditor;

        public GotoDialog(HexBox owner)
        {
            InitializeComponent();
            _hexEditor = owner;
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox1.Text))
                errorstatus = true;

            if (!errorstatus)
            {
                if (radioBegin.Checked)
                {
                    HexEditor.ScrollByteIntoView(offset);
                    HexEditor.SelectionStart = offset;
                }
                else if (radioHere.Checked)
                {
                    HexEditor.ScrollByteIntoView(HexEditor._bytePos + offset);
                    HexEditor.SelectionStart = HexEditor._bytePos + offset;
                }
                else if (radioEnd.Checked)
                {
                    HexEditor.ScrollByteIntoView(HexEditor.ByteProvider.Length - offset);
                    HexEditor.SelectionStart = HexEditor.ByteProvider.Length - offset;
                }
                this.Close();
            }
            else
            {
                this.textBox1.BackColor = Color.Red;
                this.DialogResult = DialogResult.None;
            }

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.textBox1.Text)) 
                { errorstatus = true; return; }

            try
            {
                if (btnHex.Checked)
                    this.offset = long.Parse(textBox1.Text, System.Globalization.NumberStyles.HexNumber);
                else if (btnDecimal.Checked)
                    this.offset = long.Parse(textBox1.Text);
            }
            catch (Exception error) { MessageBox.Show(error.Message); this.errorstatus = true; }


            if (offset > HexEditor.ByteProvider.Length)
                textBox1.BackColor = Color.Red;
            else
            {
                textBox1.BackColor = SystemColors.Window;
                errorstatus = false;
            }
        }
    }
}
