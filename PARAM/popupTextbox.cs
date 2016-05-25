using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Parameters
{
    public partial class popupTextbox : Form
    {
        public popupTextbox(int max)
        {
            InitializeComponent();
            numericUpDown1.Maximum = max;
        }
        public string TextVal { get { return textBox1.Text; } }
        public int ParamIndex { get { return (int)numericUpDown1.Value; } }
    }
}
