using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SALT.Graphics;

namespace XMBEditor
{
    public partial class Form1 : Form
    {
        public XMBFile OpenedFile { get; set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    OpenedFile = new XMBFile(ofd.FileName);
                    foreach(var obj in OpenedFile.Entries)
                    {
                        lstObjects.Items.Add(obj.Name);
                    }
                }
            }
        }
    }
}
