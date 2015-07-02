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
    public partial class EventLibrary : Form
    {

        public EventLibrary()
        {
            InitializeComponent();
            if (Runtime.commandDictionary != null)
                for (int i = 0; i < Runtime.commandDictionary.Count; i++)
                    listBox1.Items.Add(Runtime.commandDictionary[i].Name);

        }
        public CommandDefinition curDef;


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            numericUpDown1.Value = 0;
            comboBox1.Enabled = false;
            listBox2.Items.Clear();

            for (int i = 0; i < Runtime.commandDictionary.Count; i++)
                if (((ListBox)sender).SelectedItem.ToString() == Runtime.commandDictionary[i].Name)
                    curDef = Runtime.commandDictionary[i];
            foreach (string s in curDef.ParamSyntax)
                listBox2.Items.Add(s);

            richTextBox1.Text = richTextBox2.Text
            = curDef.EventDescription;
            numericUpDown1.Value =
            curDef.ParamSpecifiers.Count;
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
        }

    }
}
