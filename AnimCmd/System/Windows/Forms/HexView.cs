using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Be.Windows.Forms;
using System.IO;

namespace Sm4shCommand
{
    public partial class HexView : Form
    {
        public byte[] _Data;

        public HexView(byte[] Data)
        {
            this._Data = Data;
            InitializeComponent();
            hexBox.ByteProvider = new DynamicByteProvider(_Data);
        }

        private void gotoAdressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GotoDialog dlg = new GotoDialog(this.hexBox);
            dlg.ShowDialog(this);
        }


        private void hexBox_SelectionLengthChanged(object sender, EventArgs e)
        {
            statusSelLength.Text = "0x" + hexBox.SelectionLength.ToString("x");
        }

        private void hexBox_SelectionStartChanged(object sender, EventArgs e)
        {
            statusOffset.Text = "0x" + hexBox.SelectionStart.ToString("x");
        }
    }
}
