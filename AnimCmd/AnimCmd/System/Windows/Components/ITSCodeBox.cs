using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace AnimCmd
{
    [ToolboxItem(true)]
    class ITSCodeBox : RichTextBox
    {
        #region Class Members
        private List<string> dictionary;
        private ListBox listbox;
        #endregion

        #region Extern functions
        [DllImport("user32")]
        private extern static int GetCaretPos(out Point p);
        #endregion

        #region Constructors

        public ITSCodeBox()
            : base()
        {
            listbox = new ListBox();
            listbox.Parent = this;
            listbox.KeyUp += OnKeyUp;
            listbox.Visible = false;
            this.dictionary = new List<string>();
        }
        #endregion

        #region Properties
        public List<string> Dictionary
        {
            get { return this.dictionary; }
            set { this.dictionary = value; }
        }
        public string CurrentLineText
        {
            get
            {
                return Lines[CurrentLineIndex];
            }
        }
        public int CurrentLineIndex
        {
            get
            {
                Point cp;
                GetCaretPos(out cp);
                return GetLineFromCharIndex(GetCharIndexFromPosition(cp));
            }
        }
        #endregion

        #region Methods
        private static string GetLastString(string s)
        {
            string[] strArray = s.Split('\n');
            return strArray[strArray.Length - 1];
        }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            Point cp;
            GetCaretPos(out cp);
            List<string> lstTemp = new List<string>();
            listbox.SetBounds(cp.X + this.Left, cp.Y + 10, 150, 70);

            var TempFilteredList = dictionary.Where(n => n.StartsWith(CurrentLineText)).Select(r => r);

            lstTemp = TempFilteredList.ToList();
            if (TempFilteredList.Count() != 0 && !CurrentLineText.EndsWith(")") &&
                CurrentLineText != "")
            {
                listbox.DataSource = lstTemp;
                listbox.Show();
            }
            else
            {
                listbox.Hide();
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((e.KeyCode == Keys.Enter | e.KeyCode == Keys.Down | e.KeyCode == Keys.Space) && listbox.Visible == true)
            {
                listbox.Focus();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && listbox.Visible == true)
            {
                listbox.Visible = false;
                e.Handled = true;
            }
        }
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter | e.KeyCode == Keys.Space)
            {
                Point cp;
                string TempStr = this.Text.Remove(GetFirstCharIndexFromLine(CurrentLineIndex), Lines[CurrentLineIndex].Length);
                this.Text = TempStr.Insert(GetFirstCharIndexFromLine(CurrentLineIndex), ((ListBox)sender).SelectedItem.ToString());
                GetCaretPos(out cp);
                this.Select(GetFirstCharIndexFromLine(CurrentLineIndex) + CurrentLineText.Length, 0);
                listbox.Hide();
                this.Focus();
            }

        }
        #endregion
    }
}