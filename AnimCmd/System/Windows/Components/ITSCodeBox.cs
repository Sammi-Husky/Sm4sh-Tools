using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using static Sm4shCommand.Tokenizer;
using System.Text;

namespace Sm4shCommand
{
    public class ITSCodeBox : UserControl
    {
        private Timer tooltipTimer;
        private eToolTip toolTip;
        private AutoCompleteBox autocomplete;

        public ITSCodeBox(CommandList list, List<CommandInfo> commandDict)
        {
            this.SizeChanged += ITSCodeBox_SizeChanged;
            this.toolTip = new eToolTip();
            this.Cursor = Cursors.IBeam;
            this.autocomplete = new AutoCompleteBox(this);
            this.autocomplete.Parent = this;
            this.autocomplete.Visible = false;
            autocomplete.DisplayMember = "Name";
            Font = new Font(FontFamily.GenericMonospace, 9.75f);
            HorizontalScroll.Visible = this.VerticalScroll.Visible = true;
            VerticalScroll.Maximum = Math.Max(0, ClientSize.Height);
            tooltipTimer = new Timer();
            tooltipTimer.Interval = 500;
            tooltipTimer.Tick += tooltipTimer_Tick;
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            CharWidth = (int)Math.Round(MeasureChar(Font, 'A').Width);
            CharHeight = Font.Height + 2;

            // Set source after everything is setup to mitigate shenanigans
            commandDictionary = commandDict;
            autocomplete.Dictionary = commandDict;
            SetSource(list);

        }
        public void SetSource(CommandList list)
        {
            _list = list;
            Lines = new List<Line>();
            for (int i = 0; i < list.Count; i++)
                Lines.Add(new Line(list[i].ToString(), this));
            if (list.Empty)
                Lines.Add(new Line("// Empty List", this));
            DoFormat();
        }
        private void tooltipTimer_Tick(object sender, EventArgs e)
        {
            tooltipTimer.Stop();

            int yIndex = lastMouseCoords.Y.RoundDown(CharHeight) / CharHeight;

            if (yIndex >= Lines.Count | yIndex >= _list.Count)
                return;
            if (iCharFromPoint(lastMouseCoords) >= Lines[yIndex].Length)
                return;

            string str = TokenFromPoint(lastMouseCoords).Token.TrimStart();
            if (!String.IsNullOrEmpty(str))
            {
                CommandInfo cmi;
                if ((cmi = commandDictionary.FirstOrDefault(x => x.Name.StartsWith(str))) != null)
                {
                    if (cmi.EventDescription == "NONE")
                        return;

                    toolTip.ToolTipTitle = cmi.Name;
                    toolTip.ToolTipDescription = cmi.EventDescription;
                    toolTip.Show(cmi.Name, this, lastMouseCoords, 5000);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (autocomplete.Visible)
                autocomplete.Hide();

            if (ClientRectangle.Contains(e.Location) && e.Button == MouseButtons.Left)
            {
                SelectionEnd = SelectionStart = Point.Empty;
                IsDrag = true;
                SelectionStart.Y = iLineFromPoint(e.Location);

                SelectionStart = new Point(iCharFromPoint(e.Location), SelectionStart.Y);

                Point caret = CaretPosFromPoint(e.Location);
                DestroyCaret();
                CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
                SetCaretPos(caret.X, caret.Y);
                ShowCaret(Handle);
                Invalidate();
            }
        }
        Point lastMouseCoords;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (lastMouseCoords != e.Location | autocomplete.Visible)
            {
                tooltipTimer.Stop();
                tooltipTimer.Start();
                toolTip.SetToolTip(this, null);
                toolTip.Hide(this);
            }
            lastMouseCoords = e.Location;

            if (IsDrag)
            {
                SelectionEnd = new Point(iCharFromPoint(e.Location), iLineFromPoint(e.Location));
                var caret = CaretPosFromPoint(e.Location);
                SetCaretPos(caret.X, caret.Y);
                Invalidate();
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            tooltipTimer.Stop();
            toolTip.SetToolTip(this, null);
            toolTip.Hide();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            IsDrag = false;
        }


        private void ITSCodeBox_SizeChanged(object sender, EventArgs e)
        {
            UpdateRectPositions();
        }

        private void DoBackspace()
        {
            if (SelectionStart.X == 0 && SelectionStart.Y > 0)
            {
                if (!Lines[SelectionStart.Y - 1].Empty)
                {
                    var str = Lines[SelectionStart.Y].Text;
                    Lines[SelectionStart.Y - 1].Text += str;
                    Lines.RemoveAt(SelectionStart.Y);
                    CaretPrevLine();
                    CaretMoveLeft(str.Length);
                }
                else
                {
                    Lines.RemoveAt(SelectionStart.Y - 1);
                    CaretMoveUp(1);
                }
            }
            else if (SelectionStart.X > 0)
            {
                Lines[SelectionStart.Y].Text =
                    Lines[SelectionStart.Y].Text.Remove(SelectionStart.X - 1, 1);
                CaretMoveLeft(1);
            }
        }
        public StringToken TokenFromPoint(Point val)
        {
            int cIndex = iCharFromPoint(val);
            int lIndex = iLineFromPoint(val);

            return Tokenize(Lines[lIndex].Text).FirstOrDefault(x => x.Length + x.Index >= cIndex);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateRectPositions();
            IndentWidth = CharWidth * 4;
        }

        public void DoFormat()
        {
            int curindent = 0;
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Text.StartsWith("//"))
                    continue;

                if (Lines[i]._indent < 0)
                    curindent--;
                string tmp = Lines[i].Text.TrimStart();
                for (int x = 0; x < curindent; x++)
                    tmp = tmp.Insert(0, "    ");
                Lines[i].Text = tmp;
                if (Lines[i]._indent > 0)
                    curindent++;
            }
            Invalidate();
        }
        public void ApplyChanges()
        {
            CommandList.Clear();
            CommandList lst = new CommandList(CommandList.AnimationCRC);
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Text.StartsWith("//"))
                    continue;
                CommandList.Add(Lines[i].Parse());
            }
        }
        public void SetLineText(string text)
        {
            SetLineText(text, SelectionStart.Y);
        }
        public void SetLineText(string text, int iLine)
        {
            Lines[iLine].Text = text;
            CaretMoveRight(text.Length);
            Invalidate();
        }
        public void InsertText(string text)
        {
            InsertText(text, SelectionStart.X, SelectionStart.Y);
        }
        public void InsertText(string text, int iChar, int iLine)
        {
            if (iChar == 0)
                Lines[iLine].Text = text;
            else
                Lines[iLine].Text = Lines[iLine].Text.Insert(iChar, text);
            CaretMoveRight(text.Length);
            Invalidate();
        }

        #region Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CommandList CommandList { get { return _list; } set { _list = value; } }
        private CommandList _list;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CommandInfo> CommandDictionary { get { return commandDictionary; } set { commandDictionary = value; } }
        private List<CommandInfo> commandDictionary;

        public Rectangle ContentRect { get { return _contentRect; } private set { _contentRect = value; } }
        private Rectangle _contentRect;
        public Rectangle LInfoRect { get { return _lInfoRect; } private set { _lInfoRect = value; } }
        private Rectangle _lInfoRect;
        public int CharWidth { get; set; }
        public int CharHeight { get; set; }
        public float IndentWidth { get; set; }
        public Point SelectionStart;
        public Point SelectionEnd;
        #endregion
        #region Members

        bool IsDrag = false;
        public List<Line> Lines;


        #endregion
        #region Painting Methods
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            UpdateRectPositions();
            //Draw Background


            for (int i = 0; i < Lines.Count; i++)
            {
                // Line Number
                g.DrawString(i.ToString(), Font, SystemBrushes.MenuText, LInfoRect.X, CharHeight * i);

                // Selection
                //if (SelectionEnd != Point.Empty)
                //    using (var b = new SolidBrush(Color.LightSteelBlue))
                //    {
                //        if (SelectionStart.Y == SelectionEnd.Y)
                //            g.FillRectangle(b, ContentRect.X + (SelectionStart.X * CharWidth), CharHeight * SelectionEnd.Y,
                //                (SelectionEnd.X - SelectionStart.X) * CharWidth, CharHeight);
                //        else if (i == SelectionStart.Y)
                //            g.FillRectangle(b, ContentRect.X /*+ (curIndent * IndentWidth)*/ + SelectionStart.X * CharWidth, CharHeight * i,
                //                CharWidth * Lines[i].Length, CharHeight);
                //        else if (i < SelectionEnd.Y && i > SelectionStart.Y)
                //            g.FillRectangle(b, ContentRect.X /*+ (curIndent * IndentWidth)*/, CharHeight * i,
                //                Lines[i].Length * CharWidth, CharHeight);
                //        else if (i == SelectionEnd.Y)
                //            g.FillRectangle(b, ContentRect.X, CharHeight * SelectionEnd.Y,
                //                SelectionEnd.X * CharWidth, CharHeight);
                //    }

                // Text
                if (!Lines[i].Empty)
                    Lines[i].Draw(ContentRect.X, CharHeight * i, g);
                else
                    g.DrawString(Lines[i].Text, Font, Brushes.Black, ContentRect.X /*+ curIndent * IndentWidth*/, CharHeight * i);
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // Control background
            g.FillRectangle(Brushes.White, ClientRectangle);
            g.DrawRectangle(Pens.Black, ClientRectangle);
            // Draw lineInfo background
            g.FillRectangle(Brushes.LightGray, LInfoRect);
            g.DrawRectangle(Pens.Black, LInfoRect);
        }
        public new void Invalidate()
        {

            if (InvokeRequired)
                BeginInvoke(new MethodInvoker(Invalidate));
            else
                base.Invalidate();
        }
        #endregion
        #region Positioning Methods
        private float MeasureString(string str, Font font, StringFormat _fmt)
        {
            var tokens = Tokenize(str);
            float width = 0;
            using (Graphics g = CreateGraphics())
            {
                foreach (StringToken tkn in tokens)
                {
                    // Make a CharacterRange for the string's characters.
                    List<CharacterRange> range_list = new List<CharacterRange>();
                    for (int i = 0; i < tkn.Token.Length; i++)
                        range_list.Add(new CharacterRange(i, 1));
                    _fmt.SetMeasurableCharacterRanges(range_list.ToArray());

                    // Measure the string's character ranges.
                    Region[] regions = g.MeasureCharacterRanges(
                        tkn.Token, font, ContentRect, _fmt);

                    width += regions.Select(x => x.GetBounds(g)).ToArray().Sum(x => x.Width);
                }
            }
            return width;
        }
        public static SizeF MeasureChar(Font font, char c)
        {
            Size tmp1 = TextRenderer.MeasureText("M" + c.ToString() + "M", font);
            Size tmp2 = TextRenderer.MeasureText("MM", font);

            return new SizeF(tmp1.Width - tmp2.Width, font.Height);
        }
        public static SizeF MeasureString(Font font, string str)
        {
            Size tmp1 = TextRenderer.MeasureText("M" + str + "M", font);
            Size tmp2 = TextRenderer.MeasureText("MM", font);

            return new SizeF(tmp1.Width - tmp2.Width, font.Height);
        }
        private void UpdateRectPositions()
        {
            LInfoRect = new Rectangle(ClientRectangle.X, ClientRectangle.Y, (int)MeasureString(Font, Lines.Count.ToString()).Width + 4, ClientRectangle.Height);
            ContentRect = new Rectangle(LInfoRect.Width + 2, ClientRectangle.Y, ClientRectangle.Width - LInfoRect.Width, ClientRectangle.Height);
        }
        #endregion
        #region Caret Methods
        public void SetCaret(int charIndex, int lineIndex)
        {
            Point p = pointFromPos(charIndex, SelectionStart.Y);
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private void CaretMoveLeft(int num)
        {
            if (SelectionStart.X - num < 0)
            {
                CaretPrevLine();
                CaretMoveLeft(num - 1);
                return;
            }
            SelectionStart.X -= num;
            Point p;
            GetCaretPos(out p);
            p.Offset(-(CharWidth * num), 0);
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private void CaretMoveRight(int num)
        {
            if (SelectionStart.X + num > Lines[SelectionStart.Y].Length)
            {
                CaretNextLine();
                CaretMoveRight(num - 1);
                return;
            }
            SelectionStart.X += num;
            Point p;
            GetCaretPos(out p);
            p.Offset(CharWidth * num, 0);
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private void CaretMoveDown(int num)
        {
            if (SelectionStart.Y + num >= Lines.Count)
                return;
            SelectionStart.Y += num;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, CharHeight * num);
            if (SelectionStart.X > Lines[SelectionStart.Y].Length)
            {
                SelectionStart.X = Lines[SelectionStart.Y].Length;
                p.X = (SelectionStart.X * CharWidth) + ContentRect.X;
            }
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private void CaretMoveUp(int num)
        {
            if (SelectionStart.Y - num < 0)
                return;
            SelectionStart.Y -= num;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, -(CharHeight * num));
            if (SelectionStart.X > Lines[SelectionStart.Y].Length)
            {
                SelectionStart.X = Lines[SelectionStart.Y].Length;
                p.X = (SelectionStart.X * CharWidth) + ContentRect.X;
            }
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }

        private void CaretNextLine()
        {
            if (SelectionStart.Y + 1 >= Lines.Count)
                return;
            SelectionStart.Y++;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, CharHeight);
            SelectionStart.X = 0;
            p.X = (SelectionStart.X * CharWidth) + ContentRect.X;
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private void CaretPrevLine()
        {
            if (SelectionStart.Y == 0)
                return;
            SelectionStart.Y--;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, -(Font.Height + 2));
            SelectionStart.X = Lines[SelectionStart.Y].Length;
            p.X = (SelectionStart.X * CharWidth) + ContentRect.X;
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private Point CaretPosFromPoint(Point val)
        {
            int y = val.Y.Clamp(ContentRect.Y + 2, (Lines.Count - 1) * CharHeight);
            y = y.RoundDown(CharHeight);
            int x = (iCharFromPoint(val) * CharWidth) + ContentRect.X + 1;
            return new Point(x, y);
        }
        private int iCharFromPoint(Point val)
        {
            return ((int)Math.Round((float)(val.X - ContentRect.X) / CharWidth)).Clamp(0, Lines[SelectionStart.Y].Length);
        }
        private int iLineFromPoint(Point val)
        {
            int y = val.Y.Clamp(ContentRect.Y + 2, (Lines.Count - 1) * CharHeight);
            y = val.Y.RoundDown(CharHeight);
            return (y / CharHeight).Clamp(0, Lines.Count - 1);
        }
        private Point pointFromPos(int charIndex, int lineIndex)
        {
            return new Point()
            {
                X = ((charIndex * CharWidth) + ContentRect.X).Clamp(0, Lines[lineIndex].Length),
                Y = charIndex * CharHeight
            };
        }
        #endregion
        #region Key Methods
        protected override void OnKeyUp(KeyEventArgs e)
        {
            //base.OnKeyUp(e);
            if (ProcessKey(e.KeyCode) != Action.Nothing)
                return;

            Point cp;
            GetCaretPos(out cp);
            StringToken tkn = TokenFromPoint(cp);
            if (string.IsNullOrEmpty(tkn.Token) | tkn.TokType == TokenType.Seperator)
            {
                if (autocomplete.Visible)
                    autocomplete.Hide();
                return;
            }

            var filtered = CommandDictionary.Cast<CommandInfo>().Where(x =>
                x.Name.StartsWith(tkn.Token, StringComparison.InvariantCultureIgnoreCase) &
                    !tkn.Token.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            if (filtered.Length > 0)
            {
                autocomplete.DataSource = filtered;
                autocomplete.SetBounds(cp.X + CharWidth, cp.Y + CharHeight, 280, 100);
                autocomplete.Show();
            }
            else
                autocomplete.Hide();

        }
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            DoAction(ProcessKey(e.KeyCode));
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.KeyChar == '\b')
            {
                DoBackspace();
                e.Handled = true;
            }
            else if (e.KeyChar == '\r')
            {
                if (SelectionStart.X < Lines[SelectionStart.Y].Length)
                {
                    string str = Lines[SelectionStart.Y].Text.Substring(SelectionStart.X);
                    Lines[SelectionStart.Y].Text = Lines[SelectionStart.Y].Text.Remove(SelectionStart.X);
                    Lines.Insert(SelectionStart.Y + 1, new Line(str, this));
                    _list.Insert(SelectionStart.Y + 1, null);
                    CaretNextLine();
                    //CaretMoveLeft(str.Length);
                }
                else if (SelectionStart.X == Lines[SelectionStart.Y].Length)
                {
                    Lines.Insert(SelectionStart.Y + 1, new Line(string.Empty, this));
                    _list.Insert(SelectionStart.Y + 1, null);
                    CaretNextLine();
                }
                e.Handled = true;
            }
            else
            {
                Lines[SelectionStart.Y].Text = Lines[SelectionStart.Y].Text.Insert(SelectionStart.X, e.KeyChar.ToString());
                CaretMoveRight(1);
                e.Handled = true;
            }
            if (e.Handled)
                Invalidate();
        }
        private Action ProcessKey(Keys key)
        {
            switch (key)
            {
                case Keys.Left:
                    return Action.CaretLeft;
                case Keys.Right:
                    return Action.CaretRight;
                case Keys.Down:
                    return Action.CaretDown;
                case Keys.Up:
                    return Action.CaretUp;
                default:
                    return Action.Nothing;
            }
        }
        private void DoAction(Action act)
        {
            switch (act)
            {
                case Action.CaretLeft:
                    CaretMoveLeft(1);
                    return;
                case Action.CaretRight:
                    CaretMoveRight(1);
                    return;
                case Action.CaretUp:
                    CaretMoveUp(1);
                    return;
                case Action.CaretDown:
                    CaretMoveDown(1);
                    return;
                case Action.PageDown:
                    CaretMoveDown(5);
                    return;
                case Action.PageUp:
                    CaretMoveUp(5);
                    return;
            }
        }
        #endregion
        #region Native Methods
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowCaret(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyCaret();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetCaretPos(int x, int y);

        [DllImport("user32.dll")]
        static extern bool GetCaretPos(out Point lpPoint);
        #endregion
        public enum Action
        {
            Nothing,
            CaretLeft,
            CaretRight,
            CaretUp,
            CaretDown,
            PageDown,
            PageUp,
            Copy,
            Cut,
            Paste,
            SelectAll,
            Backspace
        }
    }

    public static class Tokenizer
    {
        static char[] seps = { '=', ',', '(', ')', '\n' };
        static char[] integers = { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public static StringToken[] Tokenize(string data)
        {
            List<StringToken> _tokens = new List<StringToken>();
            int i = 0;
            while (i < data.Length)
            {
                StringToken str = new StringToken();
                str.Index = i;
                if (seps.Contains(data[i]))
                    _tokens.Add(new StringToken() { Index = i, Token = data[i++].ToString(), TokType = TokenType.Seperator });
                else
                {

                    while (i < data.Length && !seps.Contains(data[i]))
                    {
                        str.Token += data[i];
                        i++;
                    }

                    if (str.Token.TrimStart().StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                        str.TokType = TokenType.Integer;
                    else if (integers.Contains(str.Token[0]))
                        str.TokType = TokenType.FloatingPoint;
                    else if (i < data.Length && data[i] == '=')
                        str.TokType = TokenType.Keyword;
                    else
                        str.TokType = TokenType.String;

                    _tokens.Add(str);
                }
            }
            return _tokens.ToArray();
        }

        public struct StringToken
        {
            public int Index { get { return _index; } set { _index = value; } }
            private int _index;
            public int Length { get { return Token.Length; } }
            public string Token { get { return _strToken; } set { _strToken = value; } }
            private string _strToken;
            public TokenType TokType { get { return _type; } set { _type = value; } }
            private TokenType _type;
            public Color TokColor
            {
                get
                {
                    switch (TokType)
                    {
                        case TokenType.String:
                            return Color.Black;
                        case TokenType.Integer:
                            return Color.DarkCyan;
                        case TokenType.FloatingPoint:
                            return Color.Red;
                        case TokenType.Keyword:
                            return Color.DarkBlue;
                        default:
                            return Color.Black;
                    }
                }
            }
        }

        public enum TokenType
        {
            String,
            Keyword,
            Integer,
            FloatingPoint,
            Seperator
        }
    }
    public class Line
    {
        public Line(string val, ITSCodeBox owner)
        {
            CodeBox = owner;
            Text = val;
        }

        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                _tokens = Tokenize(_text);
                Info = GetInfo();
            }
        }
        private string _text;
        private StringToken[] _tokens;
        public int _indent { get { return Info != null ? Info.IndentLevel : 0; } }
        public int Length { get { return Text.Length; } }
        public ITSCodeBox CodeBox { get; set; }
        public CommandInfo Info { get; set; }
        public bool Empty { get { return String.IsNullOrEmpty(Text); } }
        public CommandInfo GetInfo()
        {
            if (!Empty)
                return CodeBox.CommandDictionary?.FirstOrDefault(x =>
                     x.Name.Equals(_tokens[0].Token.TrimStart(), StringComparison.InvariantCultureIgnoreCase));
            else
                return null;
        }

        public void Draw(int x, int y, Graphics g)
        {
            Draw(x, y, CodeBox.Font, g);
        }
        public void Draw(int x, int y, Font font, Graphics g)
        {
            if (Empty)
            {
                g.DrawString(Text, CodeBox.Font, SystemBrushes.MenuText, x, y);
                return;
            }
            float posX = x;
            foreach (StringToken tkn in _tokens)
            {
                using (SolidBrush brush = new SolidBrush(tkn.TokColor))
                    g.DrawString(tkn.Token, CodeBox.Font, brush, posX, y);

                posX += tkn.Token.Length * CodeBox.CharWidth;
            }
        }
        public Command Parse()
        {
            var cmd = new Command(GetInfo());
            var parameters = _tokens.Where(x => x.TokType != TokenType.Keyword &&
                                            x.TokType != TokenType.String).ToArray();

            for (int i = 0; i < Info.ParamSpecifiers.Count; i++)
            {
                if (parameters[i].TokType == TokenType.Integer)
                    cmd.parameters.Add(int.Parse(parameters[0].Token.Remove(0, 2),
                        System.Globalization.NumberStyles.HexNumber));
                else if (Info.ParamSpecifiers[i] == 2)
                    cmd.parameters.Add(decimal.Parse(parameters[0].Token));
                else if (parameters[i].TokType == TokenType.FloatingPoint)
                    cmd.parameters.Add(float.Parse(parameters[0].Token));
            }
            return cmd;
        }
    }

    public class AutoCompleteBox : ListBox
    {
        private Timer timer;
        private eToolTip tooltip;
        public AutoCompleteBox(ITSCodeBox box)
        {
            this.Parent = Owner = box;
            this.tooltip = new eToolTip();
            this.timer = new Timer();
            this.timer.Interval = 300;
            this.timer.Tick += TtTimer_Tick;
            this.SelectedIndexChanged += AutoCompleteBox_SelectedIndexChanged;
        }

        private void AutoCompleteBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            tooltip.SetToolTip(this, null);
            tooltip.Hide();
            timer.Stop();
            timer.Start();
        }

        private void TtTimer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            CommandInfo info = SelectedItem as CommandInfo;
            var itemRect = GetItemRectangle(SelectedIndex);
            tooltip.ToolTipTitle = info.Name;
            tooltip.ToolTipDescription = info.EventDescription;
            tooltip.Show(info.Name, this, itemRect.X + this.Right, itemRect.Y, 5000);
        }

        public List<CommandInfo> Dictionary { get; set; }
        private ITSCodeBox Owner { get; set; }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space | e.KeyCode == Keys.Enter)
            {
                timer.Stop();
                tooltip.SetToolTip(this, null);
                tooltip.Hide();

                var text = $"{((CommandInfo)SelectedItem).Name}";
                Owner.SetLineText(text);
                this.Hide();
            }
            Owner.Focus();
        }
    }
}