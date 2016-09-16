using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Sm4shCommand
{
    public partial class WorkspaceWizard : Form
    {
        public WorkspaceWizard()
        {
            InitializeComponent();
            dirTextBox.Text = Application.StartupPath;
            nameTextBox.Text = "New Project";
        }
        FolderSelectDialog dlg = new FolderSelectDialog();
        public string DestinationDirectory { get { return dirTextBox.Text; } }
        public string WorkspaceName { get { return nameTextBox.Text; } }
        public string SourceDirectory { get { return srcTextBox.Text; } }

        private void btnLocation_Click(object sender, EventArgs e)
        {
            if (dlg.ShowDialog() == DialogResult.OK)
                dirTextBox.Text = dlg.SelectedPath;
        }

        private void btnWeapAdd_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add("Weapon" + (listBox1.Items.Count + 1));
            listBox1.SelectedIndex = 0;
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            RenameForm rform = new RenameForm(listBox1.Items[listBox1.SelectedIndex].ToString());
            Rectangle r = listBox1.GetItemRectangle(listBox1.SelectedIndex);
            if (r.Contains(e.Location))
            {
                if (rform.ShowDialog() == DialogResult.OK)
                    listBox1.Items[listBox1.SelectedIndex] = rform.NewName;
            }
        }

        private void btnWeapDel_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count > 0)
                listBox1.Items.RemoveAt(listBox1.Items.Count - 1);
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnSource_Click(object sender, EventArgs e)
        {

            if (dlg.ShowDialog() == DialogResult.OK)
                srcTextBox.Text = dlg.SelectedPath;
        }
    }
}
