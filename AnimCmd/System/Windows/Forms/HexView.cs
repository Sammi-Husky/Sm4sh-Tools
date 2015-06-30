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
using SmashAttacks.src.HexBox.Forms;

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
            GotoDialog dlg = new GotoDialog();
            DialogResult result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                if (dlg.radioBegin.Checked)
                    hexBox.ScrollByteIntoView(dlg.offset);
                else if (dlg.radioHere.Checked)
                    hexBox.ScrollByteIntoView(hexBox._bytePos + dlg.offset);
                else if (dlg.radioEnd.Checked)
                    hexBox.ScrollByteIntoView(hexBox.ByteProvider.Length - dlg.offset);
            }
        }

        private void hexBox_CurrentPositionInLineChanged(object sender, EventArgs e)
        {
            statusOffset.Text = "0x" + hexBox._bytePos.ToString("x");
        }

        private void hexBox_SelectionLengthChanged(object sender, EventArgs e)
        {
            statusSelLength.Text = "0x" + hexBox.SelectionLength.ToString("x");
        }
    }
}
