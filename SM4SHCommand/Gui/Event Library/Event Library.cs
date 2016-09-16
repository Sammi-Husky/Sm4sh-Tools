using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;

namespace Sm4shCommand
{
    public partial class EventLibrary : Form
    {

        public EventLibrary()
        {
            InitializeComponent();
            if (Runtime.commandDictionary == null) return;
            _list = Runtime.commandDictionary;
            listBox1.DataSource = _list;
            listBox1.DisplayMember = "Name";
        }
        public ACMD_CMD_INFO curDef
        {
            get
            {
                return (ACMD_CMD_INFO)listBox1.SelectedItem;
            }
        }
        List<ACMD_CMD_INFO> _list;

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                return;

            cboType.Enabled = false;
            listBox2.Items.Clear();

            for (int i = 0; i < curDef.ParamSpecifiers.Count; i++)
                listBox2.Items.Add(curDef.ParamSyntax.Count <= i ? "Unknown" : curDef.ParamSyntax[i]);

            richTextBox2.Text
            = curDef.EventDescription;
            numParamCount.Value =
            curDef.ParamSpecifiers.Count;
            txtName.Text = curDef.Name;
            txtIdentifier.Text = curDef.Identifier.ToString("X8");
        }
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null) return;

            cboType.Enabled = true;
            cboType.SelectedIndex = curDef.ParamSpecifiers[listBox2.SelectedIndex];
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            curDef.EventDescription = richTextBox2.Text;
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            curDef.ParamSpecifiers[listBox2.SelectedIndex] = cboType.SelectedIndex;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (curDef.ParamSpecifiers.Count < numParamCount.Value)
            {
                listBox2.Items.Add("New Parameter");
                curDef.ParamSpecifiers.Add(0);
                curDef.ParamSyntax.Add("New Parameter");
            }
            else if (curDef.ParamSpecifiers.Count > numParamCount.Value)
            {
                curDef.ParamSpecifiers.RemoveAt((int)numParamCount.Value);
                listBox2.Items.RemoveAt((int)numParamCount.Value);
                curDef.ParamSyntax.RemoveAt((int)numParamCount.Value);
            }
        }

        private void listBox2_MouseClick(object sender, MouseEventArgs e)
        {
            if (listBox2.SelectedItem == null) return;

            if (e.Button == MouseButtons.Right)
                paramContextMenu.Show(e.Location);
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null) return;

            RenameForm f = new RenameForm(listBox2.SelectedItem.ToString());
            f.ShowDialog();
            curDef.ParamSyntax[listBox2.SelectedIndex] = f.NewName;
            listBox2.Items[listBox2.SelectedIndex] = f.NewName;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;

            curDef.Name = txtName.Text;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            _list.Add(new ACMD_CMD_INFO() { Name = "New Command" });
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            listBox1.DataSource =
                _list.Where(x =>
                x.Name.ToLower().Contains(textBox2.Text.ToLower())).ToList();
        }

        private void txtIdentifier_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtIdentifier.Text))
            {
                try {
                    curDef.Identifier = UInt32.Parse(txtIdentifier.Text, System.Globalization.NumberStyles.HexNumber);
                }
                catch { MessageBox.Show("Invalid characters in textbox. Must be a hex number with digits 0-F"); txtIdentifier.Undo(); }
            }
        }

        private void EventLibrary_FormClosing(object sender, FormClosingEventArgs e)
        {
            Runtime.commandDictionary = _list;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            _list.Remove(curDef);
        }
    }
}
