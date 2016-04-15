using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noah
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Log("Waiting for commands.");
        }

        public void Log(string module, string message) =>
            richTextBox1.AppendText($"> {module} - {message}\n");

        public void Log(string message) =>
            Log("N0aH", message);
    }
}
