using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace MSBTView
{
    public unsafe partial class MainWindow : Form
    {
        VoidPtr Header;
        VoidPtr LBLHeader;
        VoidPtr Txt2Header;

        List<Block> _labelOffsets;
        List<PackedLabelEntry> _labels;
        List<int> _txtOffsets;



        public MainWindow()
        {
            InitializeComponent();
        }

        // Data handeling methods
        private string GetMessagefromOffset(VoidPtr Txt2Header, int off)
        {
            VoidPtr addr = (Txt2Header + off + 0x10);
            string s1 = "";
            while (*(byte*)addr != 0)
            {

            begin:
                if (*(byte*)addr == 0x0E)
                {
                    s1 += "%d";
                    addr += 0xA;
                    if (*(byte*)addr != 0)
                        goto begin;
                    else
                        break;
                }

                s1 += new String((sbyte*)addr);
                addr += 2;
            }

            return s1;
        }
        private PackedLabelEntry GetLabelFromOffset(VoidPtr LBLHeader, int off)
        {
            int _strLen = *(byte*)((LBLHeader + 0x10) + off);
            int _msgOffset = *(short*)((LBLHeader + 0x10) + (off + 1 + _strLen));
            string s1 = new String((sbyte*)(LBLHeader + 0x10 + off + 1)).Substring(0, _strLen);
            return new PackedLabelEntry() { _entry = s1, length = _strLen, MsgOffset = _msgOffset };
        }
        private Block GetBlockFromIndex(VoidPtr LBLHeader, int index)
        {
            int _labelCount = *(int*)((LBLHeader + 0x14) + (index * 0x08));
            int _offset = *(int*)((LBLHeader + 0x14) + (index * 0x08) + 0x04);
            return new Block() { _word0 = _labelCount, _word1 = _offset };

        }

        // Main method for parsing the MSBT files.
        public void Parse(string fName)
        {

            _labelOffsets = new List<Block>();
            _labels = new List<PackedLabelEntry>();

            Header = (new DataSource(FileMap.FromFile(fName)).Address);

            LBLHeader = (Header + 0x20);
            int _LBLtotalSize = *(int*)(LBLHeader + 0x04);
            int _sectionCount = *(int*)(LBLHeader + 0x10);

            Txt2Header = ((LBLHeader + _LBLtotalSize.RoundUp(16)) + 0x30);
            int _TXTtotalSize = *(int*)(Txt2Header + 0x04);
            int _TXTCount = *(int*)(Txt2Header + 0x10);


            if (new String((sbyte*)Header).Contains("MsgStdBn"))
            {

                _txtOffsets = new List<int>();
                for (int i = 0; i < _TXTCount; i++)
                    _txtOffsets.Add(*(int*)(Txt2Header + 0x14 + (i * 4)));

                for (int i = 0; i < _sectionCount; i++)
                {
                    Block current = GetBlockFromIndex(LBLHeader, i);
                    _labelOffsets.Add(current);

                    TreeNode Node = new TreeNode(String.Format("Group[{0}]", i.ToString("X")));


                    int offset = current._word1;
                    int l = 0;

                    while ((l < current._word0 || l == 0) && *(byte*)(LBLHeader + offset +0x10) != 0xAB)
                    {
                        // Get the Label frist, 
                        PackedLabelEntry label = GetLabelFromOffset(LBLHeader, offset);
                        TreeNode tmp = new TreeNode(label._entry);

                        // Add the message to the label node, then increment offset to next label
                        tmp.Nodes.Add(GetMessagefromOffset(Txt2Header, _txtOffsets[label.MsgOffset]));
                        Node.Nodes.Add(tmp);
                        _labels.Add(label);
                        offset += label.length + 0x05;
                        l++;
                    }
                    treeView1.Nodes.Add(Node);
                }
            }
        }

        // Various event methods
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                treeView1.Nodes.Clear();
                Parse(dlg.FileName);
            }
        }
        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Level > 0)
            {
                richTextBox1.Text = treeView1.SelectedNode.Text;
            }

        }
    }

    public class Block
    {
        public int _word0;
        public int _word1;
    }
    public class PackedLabelEntry
    {
        public int length;
        public int MsgOffset;
        public string _entry;

    }
}
