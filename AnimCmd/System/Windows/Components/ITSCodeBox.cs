using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sm4shCommand.Classes;

namespace Sm4shCommand
{
    class ITSCodeBox : RichTextBox
    {
        public ITSCodeBox(CommandList list)
        {
            AutocompleteBox = new ListBox();
            AutocompleteBox.Parent = this;
            AutocompleteBox.KeyUp += OnKeyUp;
            AutocompleteBox.Visible = false;
            ITSToolTip = new ITSToolTip();
            this.commandDictionary = Runtime.commandDictionary;

            ITSToolTip.RichTextBox = this;
            EventDescriptions = new TooltipDictionary();
            ITSToolTip.Dictionary = EventDescriptions;

            foreach (CommandInfo cd in Runtime.commandDictionary)
                if (!String.IsNullOrEmpty(cd.EventDescription))
                    EventDescriptions.Add(cd.Name, cd.EventDescription);
        }

        // Properties
        public CommandList CommandList { get { return _list; } set { _list = value; } }
        private CommandList _list;

        public List<CommandInfo> CommandDictionary { get { return commandDictionary; } set { commandDictionary = value; } }
        private List<CommandInfo> commandDictionary;

        public ITSToolTip Tooltip { get { return ITSToolTip; } set { ITSToolTip = value; } }
        private ITSToolTip ITSToolTip;

        public string CurrentLineText
        {
            get
            {
                if (Lines.Length > 0)
                    return Lines[CurrentLineIndex];
                else return "";
            }
        }
        public int CurrentLineIndex
        {
            get
            {
                Point cp;
                NativeMethods.GetCaretPos(out cp);
                return GetLineFromCharIndex(GetCharIndexFromPosition(cp));
            }
        }

        // Private Members
        private ListBox AutocompleteBox;
        private TooltipDictionary EventDescriptions;

        // Methods
        private string FomatParams(string commandName)
        {
            string Param = "";
            foreach (CommandInfo c in commandDictionary)
                if (c.Name == commandName)
                {
                    for (int i = 0; i < c.ParamSyntax.Count; i++)
                        Param += String.Format("{0}={1}", c.ParamSyntax[i], i + 1 != c.ParamSyntax.Count ? ", " : "");
                    break;
                }
            return Param;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Enter | e.KeyCode == Keys.Down | e.KeyCode == Keys.Space) && AutocompleteBox.Visible == true)
            {
                AutocompleteBox.Focus();
                if (e.KeyCode == Keys.Down && !(AutocompleteBox.SelectedIndex >= AutocompleteBox.Items.Count))
                    AutocompleteBox.SelectedIndex++;
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape | String.IsNullOrEmpty(CurrentLineText) && AutocompleteBox.Visible == true)
            {
                AutocompleteBox.Visible = false;
                e.Handled = true;
            }
        }
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter | e.KeyCode == Keys.Space)
            {
                Point cp;
                string commandName = ((ListBox)sender).SelectedItem.ToString();
                string TempStr = this.Text.Remove(GetFirstCharIndexFromLine(CurrentLineIndex), Lines[CurrentLineIndex].Length);
                NativeMethods.GetCaretPos(out cp);
                this.Text = TempStr.Insert(GetFirstCharIndexFromLine(CurrentLineIndex), commandName + String.Format("({0})", FomatParams(commandName)));
                this.SelectionStart = GetCharIndexFromPosition(cp) + Lines[GetLineFromCharIndex(GetCharIndexFromPosition(cp))].Length;
                AutocompleteBox.Hide();
                this.Focus();
            }
        }
        protected override void OnTextChanged(EventArgs e)
        {
            Point cp;
            NativeMethods.GetCaretPos(out cp);
            AutocompleteBox.SetBounds(cp.X + this.Left, cp.Y + 10, 280, 70);

            List<string> FilteredList =
                commandDictionary.Where(s => s.Name.StartsWith(CurrentLineText)).Select(m => m.Name).ToList();

            if (FilteredList.Count != 0 && !CurrentLineText.EndsWith(")") &&
                !CurrentLineText.EndsWith("(") && !String.IsNullOrEmpty(CurrentLineText))
            {
                AutocompleteBox.DataSource = FilteredList;
                AutocompleteBox.Show();
            }
            else
                AutocompleteBox.Hide();

            // Process lines.
            ProcessAllLines();

        }

        public void FormatLine(string[] lines, int LineIndex)
        {
            if (lines.Length == 0)
                return;

            string line = lines[LineIndex];

            // Save the position and make the whole line black
            int nPosition = SelectionStart;
            SelectionStart = GetFirstCharIndexFromLine(LineIndex);
            SelectionLength = line.Length;
            SelectionColor = Color.Black;

            // Process numbers
            Format(line, LineIndex, "\\b(?:[0-9]*\\.)?[0-9]+\\b", Color.Red); // Decimal
            Format(line, LineIndex, @"\b0x[a-fA-F\d]+\b", Color.DarkCyan); // Hexadecimal

            // Don't need to process these, they just slow everything down.

            // Process parenthesis
            Format(line, LineIndex, "[\x28-\x2c]", Color.Blue);
            //// Process comments
            Format(line, LineIndex, "\"[^\"]*\"", Color.DarkRed);

            SelectionStart = nPosition;
            SelectionLength = 0;
            SelectionColor = Color.Black;
        }
        private void Format(string line, int LineIndex, string strRegex, Color color)
        {

            Regex regKeywords = new Regex(strRegex, RegexOptions.IgnoreCase);
            Match regMatch;

            for (regMatch = regKeywords.Match(line); regMatch.Success; regMatch = regMatch.NextMatch())
            {
                // Process the words
                int nStart = GetFirstCharIndexFromLine(LineIndex) + regMatch.Index;
                int nLenght = regMatch.Length;
                SelectionStart = nStart;
                SelectionLength = nLenght;
                SelectionColor = color;
            }

        }
        public void ProcessAllLines()
        {
            BeginUpdate();
            string[] lines = Lines;
            for (int i = 0; i < lines.Length; i++)
                FormatLine(lines, i);
            EndUpdate();
        }

        private IntPtr OldEventMask;
        public void BeginUpdate()
        {
            NativeMethods.SendMessage(this.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            OldEventMask = (IntPtr)NativeMethods.SendMessage(this.Handle, NativeMethods.EM_SETEVENTMASK, IntPtr.Zero, IntPtr.Zero);
        }
        public void EndUpdate()
        {
            NativeMethods.SendMessage(this.Handle, NativeMethods.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            NativeMethods.SendMessage(this.Handle, NativeMethods.EM_SETEVENTMASK, IntPtr.Zero, OldEventMask);
        }
    }

    internal sealed class NativeMethods
    {
        [DllImport("user32")]
        public extern static int GetCaretPos(out Point p);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public const int WM_USER = 0x0400;
        public const int EM_SETEVENTMASK = (WM_USER + 69);
        public const int WM_SETREDRAW = 0x0b;
    }
}