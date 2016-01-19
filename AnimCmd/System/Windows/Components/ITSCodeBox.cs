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
        public ITSCodeBox(CommandList list)
        {
            _list = list;
            Lines = list.Select(x => x.ToString()).ToList();
            this.Load += ITSCodeBox_Load;
            this.SizeChanged += ITSCodeBox_SizeChanged;
            this.Cursor = Cursors.IBeam;
            this.KeyPress += ITSCodeBox_KeyPress;
            this.MouseClick += ITSCodeBox_MouseClick;
        }

        private void ITSCodeBox_MouseClick(object sender, MouseEventArgs e)
        {
            Point p = e.Location;
            if (ClientRectangle.Contains(p))
            {
                int y = p.Y.Clamp(_rectContent.Y + 2, (Lines.Count - 1) * (Font.Height + 2));
                y = y.RoundDown(Font.Height + 2);
                curLine = (y / (Font.Height + 2)).Clamp(0, Lines.Count - 1);
                SizeF size = GetCharSize(new Font("Courier New", 9.75f), 'M');
                int CharWidth = (int)Math.Round(size.Width * 1f /*0.85*/) - 1 /*0*/;
                int x = p.X.Clamp(_rectContent.X + 2, _rectContent.X + 2 + (int)MeasureString(Lines[curLine], Font));
                x = (int)Math.Round((float)e.Location.X / CharWidth);

                DestroyCaret();
                CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
                SetCaretPos(x, y);
                ShowCaret(Handle);
            }
        }

        private void ITSCodeBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\b' && !string.IsNullOrEmpty(Lines[curLine]))
            {
                Lines[curLine] = Lines[curLine].Substring(0, caretPosInLine - 1) + Lines[curLine].Substring(caretPosInLine + 1);
                CaretMoveLeft();
            }
            else if (e.KeyChar == '\r')
                Lines.Insert(++curLine, "");
            else
            {
                Lines[curLine] += e.KeyChar;
                CaretMoveRight();
            }
            Invalidate();
        }
        private void ITSCodeBox_SizeChanged(object sender, EventArgs e)
        {
            UpdateRectanglePosition();
        }
        private void ITSCodeBox_Load(object sender, EventArgs e)
        {
            UpdateRectanglePosition();
            IndentWidth = CreateGraphics().MeasureString(" ", Font).Width * 5;
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

        /// <summary>
        /// Indent width 
        /// </summary>
        private float IndentWidth;
        public List<string> Lines;

        private int curIndent = 0;
        private int curLine = 0;

        private int caretPosInLine = 0;


        private StringFormat _stringFormat = new StringFormat()
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.NoWrap
        };
        #endregion
        #region Painting Methods
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            //Draw Background
            g.FillRectangle(new SolidBrush(Color.White), ClientRectangle);
            g.DrawRectangle(Pens.Black, ClientRectangle);
            // Line numbers
            PaintLineInfo(g);

            for (int i = 0; i < Lines.Count; i++)
            {
                //if (_list[i]._commandInfo?.IndentLevel < 0)
                //    curIndent--;

                if (!string.IsNullOrEmpty(Lines[i]))
                    DrawTokenizedLine(Tokenize(Lines[i]), _rectContent.X + curIndent * IndentWidth, (Font.Height + 2) * i, g);
                else
                    g.DrawString(Lines[i], Font, Brushes.Black, _rectContent.X + curIndent * IndentWidth, (Font.Height + 2) * i);

                //if (_list[i]._commandInfo?.IndentLevel > 0)
                //    curIndent++;
            }
        }
        private void PaintLineInfo(Graphics g)
        {
            float width = MeasureString(Lines.Count.ToString(), Font);
            _rectLineInfo = new Rectangle(ClientRectangle.Left, 0, (int)width * 2, ClientRectangle.Height);
            _rectContent.X = _rectLineInfo.Width + 2;
            _rectContent.Width -= _rectLineInfo.Width;

            //Draw linInfo background
            g.FillRectangle(Brushes.LightGray, _rectLineInfo);
            g.DrawRectangle(Pens.Black, _rectLineInfo);

            for (int i = 0; i < Lines.Count; i++)
                g.DrawString(i.ToString(), Font, SystemBrushes.MenuText, _rectLineInfo.X + 2, (Font.Height + 2) * i);
        }
        private void DrawTokenizedLine(StringToken[] lineTokens, float x, float y, Graphics g)
        {
            float posX = x;
            foreach (StringToken tkn in lineTokens)
            {
                using (SolidBrush brush = new SolidBrush(tkn.TokColor))
                    g.DrawString(tkn.Token, Font, brush, posX, y, _stringFormat);

                posX += MeasureString(tkn.Token, Font);
            }
        }
        #endregion
        #region Positioning Methods
        private float MeasureString(string str, Font font)
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
                    StringFormat stfmt = _stringFormat;
                    stfmt.SetMeasurableCharacterRanges(range_list.ToArray());

                    // Measure the string's character ranges.
                    Region[] regions = g.MeasureCharacterRanges(
                        tkn.Token, font, _rectContent, _stringFormat);

                    width += regions.Select(x => x.GetBounds(g)).ToArray().Sum(x => x.Width);
                }
            }
            return width;
        }
        public static SizeF GetCharSize(Font font, char c)
        {
            Size sz2 = TextRenderer.MeasureText("<" + c.ToString() + ">", font);
            Size sz3 = TextRenderer.MeasureText("<>", font);

            return new SizeF(sz2.Width - sz3.Width + 1, /*sz2.Height*/font.Height);
        }
        private void UpdateRectanglePosition()
        {
            _rectLineInfo = new Rectangle(ClientRectangle.X, ClientRectangle.Y, (int)MeasureString(Lines.Count.ToString(), Font), ClientRectangle.Height);
            _rectContent = new Rectangle(_rectLineInfo.Width + 2, ClientRectangle.Y, ClientRectangle.Width - _rectLineInfo.Width, ClientRectangle.Height);
        }
        private void CaretMoveLeft()
        {
            Point p;
            GetCaretPos(out p);
            int y = (p.Y - 1).RoundDown(Font.Height + 2);
            int x = (p.X - 1).RoundDown((int)MeasureString(" ", Font));
            y = y.Clamp(2, Lines.Count * (Font.Height + 2));
            x = x.Clamp(2, (int)MeasureString(Lines[curLine] + _rectLineInfo.Width + 2, Font));
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(x, y);
            ShowCaret(Handle);
        }
        private void CaretMoveRight()
        {
            Point p;
            GetCaretPos(out p);
            int y = (p.Y + 1).RoundDown(Font.Height + 2);
            int x = (p.X + 1).RoundDown((int)MeasureString(" ", Font));
            y = y.Clamp(2, Lines.Count * (Font.Height + 2));
            x = x.Clamp(2, (int)MeasureString(Lines[curLine] + _rectLineInfo.Width + 2, Font));
            DestroyCaret();
            CreateCaret(Handle, IntPtr.Zero, 1, Font.Height);
            SetCaretPos(x, y);
            ShowCaret(Handle);
        }
        #endregion

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
    }
}
public static class Tokenizer
{
    static char[] seps = { '=', ',', '(', ')', '\n' };
    static char[] integers = { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    //static char[] hexNums = { 'A', 'B', 'C', 'D', 'E', 'F' };

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