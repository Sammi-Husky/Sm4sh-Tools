using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SmashAttacks.src.HexBox.Forms
{
    public partial class GotoDialog : Form
    {
        public long offset;
        bool errorstatus = false;

        public GotoDialog()
        {
            InitializeComponent();
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            try
            { this.offset = long.Parse(textBox1.Text, System.Globalization.NumberStyles.HexNumber); }
            catch (Exception error)
            { MessageBox.Show(error.Message); this.errorstatus = true; }
            if (!errorstatus)
                this.Close();
            
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
