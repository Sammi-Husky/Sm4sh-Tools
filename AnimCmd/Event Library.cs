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
    }
}
