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
            if (Runtime.commandDictionary != null)
                for (int i = 0; i < Runtime.commandDictionary.Count; i++)
                    listBox1.Items.Add(Runtime.commandDictionary[i].Name);

        }
        public CommandInfo curDef
        {
            get
            {
                if (listBox1.SelectedItem != null)
                {
                    CommandInfo tmp = null;
                    int i = 0;
                    while (tmp == null)
                    {
                        if (listBox1.SelectedItem.ToString() == Runtime.commandDictionary[i].Name)
                            tmp = Runtime.commandDictionary[i];
                        i++;
                    }
                    return tmp;
                }
                else
                    return null;
            }
        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                return;

            comboBox1.Enabled = false;
            listBox2.Items.Clear();

            for (int i = 0; i < curDef.ParamSpecifiers.Count; i++)
            {
                if (curDef.ParamSyntax.Count <= i)
                    listBox2.Items.Add("Unknown");
                else
                    listBox2.Items.Add(curDef.ParamSyntax[i]);
            }
            richTextBox1.Text = richTextBox2.Text
            = curDef.EventDescription;
            numericUpDown1.Value =
            curDef.ParamSpecifiers.Count;
            textBox1.Text = listBox1.SelectedItem.ToString();
        }
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem != null)
            {
                comboBox1.Enabled = true;
                comboBox1.SelectedIndex = curDef.ParamSpecifiers[listBox2.SelectedIndex];
            }
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            curDef.EventDescription = richTextBox1.Text = richTextBox2.Text;
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            curDef.ParamSpecifiers[listBox2.SelectedIndex] = comboBox1.SelectedIndex;
        }


        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (curDef.ParamSpecifiers.Count < numericUpDown1.Value)
            {
                listBox2.Items.Add("New Parameter");
                curDef.ParamSpecifiers.Add(0);
                curDef.ParamSyntax.Add("New Parameter");
            }
            else if (curDef.ParamSpecifiers.Count > numericUpDown1.Value)
            {
                curDef.ParamSpecifiers.RemoveAt((int)numericUpDown1.Value);
                listBox2.Items.RemoveAt((int)numericUpDown1.Value);
                curDef.ParamSyntax.RemoveAt((int)numericUpDown1.Value);
            }
        }

        private void listBox2_MouseClick(object sender, MouseEventArgs e)
        {
            if (listBox2.SelectedItem != null)
                if (e.Button == MouseButtons.Right)
                    paramContextMenu.Show(e.Location);

        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem != null)
            {
                RenameForm f = new RenameForm(listBox2.SelectedItem.ToString());
                f.ShowDialog();
                curDef.ParamSyntax[listBox2.SelectedIndex] = f.NewName;
                listBox2.Items[listBox2.SelectedIndex] = f.NewName;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                curDef.Name = textBox1.Text;
                listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
            }
        }
    }
}
