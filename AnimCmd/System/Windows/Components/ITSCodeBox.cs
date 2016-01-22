using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using static Tokenizer;
using System.Text;

namespace Sm4shCommand
{
    public class ITSCodeBox : UserControl
    {
        private Timer _tooltipTimer;
        public ITSCodeBox(CommandList list)
        {
            _list = list;
            Lines = list.Select(x => x.ToString()).ToList();
            this.SizeChanged += ITSCodeBox_SizeChanged;
            this.Cursor = Cursors.IBeam;
            this.KeyPress += ITSCodeBox_KeyPress;
            this.PreviewKeyDown += ITSCodeBox_PreviewKeyDown;
            this.Font = new Font(FontFamily.GenericMonospace, 9.75f);
            this.HorizontalScroll.Visible = this.VerticalScroll.Visible = true;
            this.VerticalScroll.Maximum = Math.Max(0, ClientSize.Height);
            this._tooltipTimer = new Timer();
            _tooltipTimer.Interval = 500;
            _tooltipTimer.Tick += _tooltipTimer_Tick;
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            CharWidth = (int)Math.Round(MeasureChar(Font, 'A').Width);
            CharHeight = Font.Height + 2;
        }

        private void _tooltipTimer_Tick(object sender, EventArgs e)
        {
            _tooltipTimer.Stop();
            int yIndex = oldLocation.Y.RoundDown(CharHeight) / CharHeight;

            if (yIndex >= Lines.Count | yIndex >= _list.Count)
                return;
            if (iCharFromPoint(oldLocation) >= Lines[yIndex].Length)
                return;

            if (!String.IsNullOrEmpty(_list[yIndex]._commandInfo?.EventDescription))
            {
                CommandInfo cmi = _list[yIndex]._commandInfo;
                toolTip.ToolTipTitle = cmi.Name;
                toolTip.ToolTipDescription = cmi.EventDescription;
                toolTip.Show(cmi.Name, this, oldLocation, 5000);
            }
        }

        private void ITSCodeBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    CaretMoveLeft(1);
                    return;
                case Keys.Right:
                    CaretMoveRight(1);
                    return;
                case Keys.Down:
                    CaretMoveDown(1);
                    return;
                case Keys.Up:
                    CaretMoveUp(1);
                    return;
            }
        }
        Point oldLocation;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (ClientRectangle.Contains(e.Location) && e.Button == MouseButtons.Left)
            {
                SelectionEnd = SelectionStart = Point.Empty;
                IsDrag = true;
                LineIndex = iLineFromPoint(e.Location);

                SelectionStart.Y = LineIndex;
                SelectionStart.X = iCharFromPoint(e.Location);

                Point caret = CaretPosFromPoint(e.Location);
                DestroyCaret();
                CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
                SetCaretPos(caret.X, caret.Y);
                ShowCaret(Handle);
                Invalidate();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!ClientRectangle.Contains(e.Location))
            {
                _tooltipTimer.Stop();
                toolTip.SetToolTip(this, null);
                toolTip.Hide();
                return;
            }
            if (oldLocation != e.Location)
            {
                _tooltipTimer.Stop();
                _tooltipTimer.Start();
                toolTip.SetToolTip(this, null);
                toolTip.Hide(this);
            }
            oldLocation = e.Location;

            if (IsDrag)
            {
                SelectionEnd.X = iCharFromPoint(e.Location);
                SelectionEnd.Y = iLineFromPoint(e.Location);
                var caret = CaretPosFromPoint(e.Location);
                SetCaretPos(caret.X, caret.Y);
                Invalidate();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            IsDrag = false;
        }

        private void ITSCodeBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\b')
            {
                DoBackspace();
                e.Handled = true;
            }
            else if (e.KeyChar == '\r')
            {
                if (SelectionStart.X < Lines[LineIndex].Length)
                {
                    string str = Lines[LineIndex].Substring(SelectionStart.X);
                    Lines[LineIndex] = Lines[LineIndex].Remove(SelectionStart.X);
                    Lines.Insert(LineIndex + 1, str);
                    CaretNextLine();
                    //CaretMoveLeft(str.Length);
                }
                else if (SelectionStart.X == Lines[LineIndex].Length)
                {
                    Lines.Insert(LineIndex + 1, string.Empty);
                    CaretNextLine();
                }
                e.Handled = true;
            }
            else
            {
                Lines[LineIndex] = Lines[LineIndex].Insert(SelectionStart.X, e.KeyChar.ToString());
                CaretMoveRight(1);
                e.Handled = true;
            }
            if (e.Handled)
                Invalidate();
        }
        private void ITSCodeBox_SizeChanged(object sender, EventArgs e)
        {
            UpdateRectPositions();
        }
        private void DoBackspace()
        {
            if (SelectionStart.X == 0 && LineIndex > 0)
            {
                if (!String.IsNullOrEmpty(Lines[LineIndex - 1]))
                {
                    var str = Lines[LineIndex];
                    Lines[LineIndex - 1] += str;
                    Lines.RemoveAt(LineIndex);
                    CaretPrevLine();
                    CaretMoveLeft(str.Length);
                }
                else
                {
                    Lines.RemoveAt(LineIndex - 1);
                    CaretMoveUp(1);
                }
            }
            else if (SelectionStart.X > 0)
            {
                Lines[LineIndex] = Lines[LineIndex].Remove(SelectionStart.X - 1, SelectionStart.X - SelectionEnd.X);
                CaretMoveLeft(1);
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateRectPositions();
            IndentWidth = CreateGraphics().MeasureString(" ", Font).Width * 5;
        }
        public new void Invalidate()
        {

            if (InvokeRequired)
                BeginInvoke(new MethodInvoker(Invalidate));
            else
                base.Invalidate();
        }

        #region Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CommandList CommandList { get { return _list; } set { _list = value; } }
        private CommandList _list;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CommandInfo> CommandDictionary { get { return commandDictionary; } set { commandDictionary = value; } }
        private List<CommandInfo> commandDictionary;
        #endregion
        #region Members
        Rectangle _rectLineInfo;
        Rectangle _rectContent;

        public eToolTip toolTip = new eToolTip();

        int CharWidth;
        int CharHeight;
        bool IsDrag = false;
        /// <summary>
        /// Indent width 
        /// </summary>
        private float IndentWidth;
        public List<string> Lines;

        private int curIndent = 0;
        private int LineIndex = 0;
        private Point SelectionStart = new Point();
        private Point SelectionEnd = new Point();

        #endregion
        #region Painting Methods
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            UpdateRectPositions();
            //Draw Background
            g.FillRectangle(Brushes.White, ClientRectangle);
            g.DrawRectangle(Pens.Black, ClientRectangle);
            //Draw linInfo background
            g.FillRectangle(Brushes.LightGray, _rectLineInfo);
            g.DrawRectangle(Pens.Black, _rectLineInfo);

            for (int i = 0; i < Lines.Count; i++)
            {
                // Line Number
                g.DrawString(i.ToString(), Font, SystemBrushes.MenuText, _rectLineInfo.X, CharHeight * i);

                //if (SelectionEnd != Point.Empty)
                //    using (var b = new SolidBrush(Color.LightSteelBlue))
                //    {
                //        if (i == SelectionEnd.Y)
                //            g.FillRectangle(b, _rectContent.X + (curIndent * IndentWidth) + (SelectionStart.X * CharWidth), CharHeight * i,
                //                (SelectionEnd.X - SelectionStart.X) * CharWidth, CharHeight);
                //        else if (i == SelectionStart.Y)
                //            g.FillRectangle(b, _rectContent.X + (curIndent * IndentWidth) + SelectionStart.X * CharWidth, CharHeight * i,
                //                CharWidth * Lines[i].Length, CharHeight);
                //        else if (i < SelectionEnd.Y)
                //            g.FillRectangle(b, _rectContent.X + (curIndent * IndentWidth), CharHeight * i,
                //                Lines[i].Length * CharWidth, CharHeight);
                //    }

                //// Dont want to indent the command that reduces indent
                //if (i <= _list.Count && _list[i]._commandInfo?.IndentLevel < 0)
                //    curIndent--;

                if (!string.IsNullOrEmpty(Lines[i]))
                    DrawTokenizedLine(Tokenize(Lines[i]), _rectContent.X + curIndent * IndentWidth, CharHeight * i, g);
                else
                    g.DrawString(Lines[i], Font, Brushes.Black, _rectContent.X + curIndent * IndentWidth, CharHeight * i);

                //// Indent after indenting command
                //if (i <= _list.Count && _list[i]._commandInfo?.IndentLevel > 0)
                //    curIndent++;
            }
        }
        private void DrawTokenizedLine(StringToken[] lineTokens, float x, float y, Graphics g)
        {
            float posX = x;
            foreach (StringToken tkn in lineTokens)
            {
                using (SolidBrush brush = new SolidBrush(tkn.TokColor))
                    g.DrawString(tkn.Token, Font, brush, posX, y);

                posX += tkn.Token.Length * CharWidth;
            }
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
                        tkn.Token, font, _rectContent, _fmt);

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
            _rectLineInfo = new Rectangle(ClientRectangle.X, ClientRectangle.Y, (int)MeasureString(Font, Lines.Count.ToString()).Width + 4, ClientRectangle.Height);
            _rectContent = new Rectangle(_rectLineInfo.Width + 2, ClientRectangle.Y, ClientRectangle.Width - _rectLineInfo.Width, ClientRectangle.Height);
        }
        #endregion
        #region Caret Methods
        private void SetCaret(int charIndex, int lineIndex)
        {
            Point p = pointFromPos(charIndex, lineIndex);
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
            if (SelectionStart.X + num > Lines[LineIndex].Length)
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
            if (LineIndex + num >= Lines.Count)
                return;
            LineIndex += num;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, CharHeight * num);
            if (SelectionStart.X > Lines[LineIndex].Length)
            {
                SelectionStart.X = Lines[LineIndex].Length;
                p.X = (SelectionStart.X * CharWidth) + _rectContent.X;
            }
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private void CaretMoveUp(int num)
        {
            if (LineIndex - num < 0)
                return;
            LineIndex -= num;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, -(CharHeight * num));
            if (SelectionStart.X > Lines[LineIndex].Length)
            {
                SelectionStart.X = Lines[LineIndex].Length;
                p.X = (SelectionStart.X * CharWidth) + _rectContent.X;
            }
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }

        private void CaretNextLine()
        {
            if (LineIndex + 1 >= Lines.Count)
                return;
            LineIndex++;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, CharHeight);
            SelectionStart.X = 0;
            p.X = (SelectionStart.X * CharWidth) + _rectContent.X;
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private void CaretPrevLine()
        {
            if (LineIndex == 0)
                return;
            LineIndex--;

            Point p;
            GetCaretPos(out p);
            p.Offset(0, -(Font.Height + 2));
            SelectionStart.X = Lines[LineIndex].Length;
            p.X = (SelectionStart.X * CharWidth) + _rectContent.X;
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(p.X, p.Y);
            ShowCaret(Handle);
        }
        private Point CaretPosFromPoint(Point val)
        {
            int y = val.Y.Clamp(_rectContent.Y + 2, (Lines.Count - 1) * CharHeight);
            y = y.RoundDown(CharHeight);
            int x = (iCharFromPoint(val) * CharWidth) + _rectContent.X + 1;
            return new Point(x, y);
        }
        private int iCharFromPoint(Point val)
        {
            return ((int)Math.Round((float)(val.X - _rectContent.X) / CharWidth)).Clamp(0, Lines[LineIndex].Length);
        }
        private int iLineFromPoint(Point val)
        {
            int y = val.Y.Clamp(_rectContent.Y + 2, (Lines.Count - 1) * CharHeight);
            y = val.Y.RoundDown(CharHeight);
            return (y / CharHeight).Clamp(0, Lines.Count - 1);
        }
        private Point pointFromPos(int charIndex, int lineIndex)
        {
            return new Point()
            {
                X = ((charIndex * CharWidth) + _rectContent.X).Clamp(0, Lines[lineIndex].Length),
                Y = charIndex * CharHeight
            };
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

            if (seps.Contains(data[i]))
                _tokens.Add(new StringToken() { Token = data[i++].ToString(), TokType = TokenType.String });
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
                    case TokenType.Decimal:
                        return Color.Red;
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
        Decimal
    }
}