using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using System.Text;

namespace Sm4shCommand
{
    public class ITSCodeBox : RichTextBox
    {
        public ITSCodeBox()
        {
            Init();
        }
        public ITSCodeBox(CommandList list)
        {
            Init();
            DisplayScript(list);
        }

        // Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CommandList CommandList { get { return _list; } set { _list = value; } }
        private CommandList _list;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CommandInfo> CommandDictionary { get { return commandDictionary; } set { commandDictionary = value; } }
        private List<CommandInfo> commandDictionary;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ITSToolTip Tooltip { get { return ITSToolTip; } set { ITSToolTip = value; } }
        private ITSToolTip ITSToolTip;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentLineText
        {
            get
            {
                if (Lines.Length > 0)
                    return Lines[CurrentLineIndex];
                else return "";
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        int curIndent = 0;
        bool processing = false;
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
        public CommandList ApplyChanges()
        {
            // Don't bother selectively processing events, just clear and repopulate the whole thing.
            string[] lines = Lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("//")).Select(x => x.Trim()).ToArray();
            _list.Clear();

            UnknownCommand unkC = null;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("0x"))
                {
                    if (unkC == null)
                        unkC = new UnknownCommand();
                    unkC.data.Add(Int32.Parse(lines[i].Substring(2, 8), System.Globalization.NumberStyles.HexNumber));
                    continue;
                }
                foreach (CommandInfo e in Runtime.commandDictionary)
                    if (lines[i].StartsWith(e.Name))
                    {
                        if (unkC != null)
                        {
                            _list.Add(unkC);
                            unkC = null;
                        }
                        string temp = lines[i].Substring(lines[i].IndexOf('(')).Trim(new char[] { '(', ')' });
                        List<string> Params = temp.Replace("0x", "").Split(',').ToList();
                        Command c = new Command(e);
                        for (int counter = 0; counter < e.ParamSpecifiers.Count; counter++)
                        {
                            // parameter - it's syntax keyword(s), and then parse.
                            if (e.ParamSyntax.Count > 0)
                                Params[counter] = Params[counter].Substring(Params[counter].IndexOf('=') + 1);

                            if (e.ParamSpecifiers[counter] == 0)
                                c.parameters.Add(Int32.Parse(Params[counter], System.Globalization.NumberStyles.HexNumber));
                            else if (e.ParamSpecifiers[counter] == 1)
                                c.parameters.Add(float.Parse(Params[counter]));
                            else if (e.ParamSpecifiers[counter] == 2)
                                c.parameters.Add(decimal.Parse(Params[counter]));
                        }
                        _list.Add(c);
                    }
            }
            CommandList = _list;
            return _list;
        }
        public void DisplayScript(CommandList list)
        {
            curIndent = 0;
            StringBuilder sb = new StringBuilder();
            foreach (Command cmd in list)
            {

                if (cmd._commandInfo?.IndentLevel < 0)
                    curIndent--;

                var str = cmd + "\n";
                for (int i = 0; i < curIndent; i++)
                    str = str.Insert(0, "\t");
                sb.Append(str);
                if (cmd._commandInfo?.IndentLevel > 0)
                    curIndent++;
            }

            if (list.Empty)
                sb.Append("//    Empty list    //");
            Text = sb.ToString();
            CommandList = list;
            BeginUpdate();
            ProcessAllLines();
            EndUpdate();
        }

        private void Autocomplete_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter | e.KeyCode == Keys.Space)
            {
                string commandName = ((ListBox)sender).SelectedItem.ToString();
                CommandInfo info = CommandDictionary.Find(x => x.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                int curIndex = GetFirstCharIndexFromLine(CurrentLineIndex) + curIndent;
                string TempStr = Text.Remove(curIndex, Lines[CurrentLineIndex].Length);
                Text = TempStr.Insert(curIndex, commandName + String.Format("({0})", FomatParams(commandName)));
                SelectionStart = curIndex + CurrentLineText.Length;
                AutocompleteBox.Hide();
            }
        }
        private void Autocomplete_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter | e.KeyCode == Keys.Down | e.KeyCode == Keys.Space)
            {
                AutocompleteBox.Focus();
                if (e.KeyCode == Keys.Down && !(AutocompleteBox.SelectedIndex >= AutocompleteBox.Items.Count))
                    AutocompleteBox.SelectedIndex++;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape | String.IsNullOrEmpty(CurrentLineText))
            {
                AutocompleteBox.Hide();
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                return;

            Point cp;
            NativeMethods.GetCaretPos(out cp);
            AutocompleteBox.SetBounds(cp.X + this.Left, cp.Y + 10, 280, 70);

            List<string> FilteredList =
                commandDictionary.Where(s => s.Name.StartsWith(CurrentLineText.TrimStart(), StringComparison.InvariantCultureIgnoreCase)
                & !string.IsNullOrEmpty(CurrentLineText.TrimStart())).Select(m => m.Name).ToList();

            if (FilteredList.Count > 0)
            {
                AutocompleteBox.DataSource = FilteredList;
                AutocompleteBox.Show();
                AutocompleteBox.Update();
            }
            else
                AutocompleteBox.Hide();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (AutocompleteBox.Visible)
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        AutocompleteBox.Focus();
                        Autocomplete_OnKeyUp(AutocompleteBox, e);
                        Focus();
                        e.Handled = e.SuppressKeyPress = true;
                        break;
                    case Keys.Down:
                        if (AutocompleteBox.SelectedIndex != AutocompleteBox.Items.Count - 1)
                            AutocompleteBox.SelectedIndex++;
                        e.Handled = e.SuppressKeyPress = true;
                        break;
                    case Keys.Up:
                        if (AutocompleteBox.SelectedIndex != 0)
                            AutocompleteBox.SelectedIndex--;
                        e.Handled = e.SuppressKeyPress = true;
                        break;
                }
            }
        }
        protected override void OnTextChanged(EventArgs e)
        {
            if (processing) return;

            BeginUpdate();
            ProcessAllLines();
            EndUpdate();
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

            // Process parenthesis
            Format(line, LineIndex, "[\x28-\x2c]", Color.Blue);
            // Process comments
            Format(line, LineIndex, "^/[/|*](.+)$", Color.DarkRed); // Line comments

            SelectionStart = nPosition;
            SelectionLength = 0;
            SelectionColor = Color.Black;
        }
        private void Format(string lineText, int LineIndex, string strRegex, Color color)
        {
            Regex regKeywords = new Regex(strRegex, RegexOptions.IgnoreCase);
            Match regMatch;

            for (regMatch = regKeywords.Match(lineText); regMatch.Success; regMatch = regMatch.NextMatch())
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
            processing = true;
            string[] lines = Lines;
            for (int i = 0; i < lines.Length; i++)
                FormatLine(lines, i);
            processing = false;
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

        private void Init()
        {
            WordWrap = false;
            AcceptsTab = true;
            Font = new Font("Tahoma", 9.5f);
            AutocompleteBox = new ListBox();
            AutocompleteBox.Parent = this;
            AutocompleteBox.KeyUp += Autocomplete_OnKeyUp;
            AutocompleteBox.KeyDown += Autocomplete_OnKeyDown;
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