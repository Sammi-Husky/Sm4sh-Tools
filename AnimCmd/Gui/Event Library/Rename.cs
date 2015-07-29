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
    public partial class RenameForm : Form
    {
        public RenameForm(string text)
        {
            InitializeComponent();
            textBox1.Text = text;
        }
        public string ParameterName
        {
            get { return textBox1.Text; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }
    }
}
