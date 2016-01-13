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
    public class ITSCodeBox : UserControl
    {
        public ITSCodeBox(CommandList list)
        {
            _list = list;
        }
        // Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CommandList CommandList { get { return _list; } set { _list = value; } }
        private CommandList _list;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CommandInfo> CommandDictionary { get { return commandDictionary; } set { commandDictionary = value; } }
        private List<CommandInfo> commandDictionary;
        Rectangle _rectLineInfo
        {
            get
            {
                return new Rectangle(ClientRectangle.Left, 0, 15, ClientRectangle.Height);
            }
        }
        Rectangle _rectContent
        {
            get
            {
                return new Rectangle(_rectLineInfo.Width, 0, ClientRectangle.Width - _rectLineInfo.Width, ClientRectangle.Height);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics gr = e.Graphics;
            // Draw Background
            e.Graphics.FillRectangle(new SolidBrush(Color.White), ClientRectangle);
            e.Graphics.DrawRectangle(Pens.Black, ClientRectangle);
            //Draw linInfo background
            e.Graphics.FillRectangle(Brushes.LightGray, _rectLineInfo);
            e.Graphics.DrawRectangle(Pens.Black, _rectLineInfo);
            for (int i = 0; i < _list.Count; i++)
            {

                e.Graphics.DrawString(i.ToString(), Font, SystemBrushes.MenuText, _rectLineInfo.X + 2, (Font.Height + 2) * i);

                Command cmd = _list[i];
                StringToken[] tokens = Tokenizer.Tokenize(cmd.ToString());


                float posX = _rectLineInfo.Width + 5;
                foreach (StringToken tkn in tokens)
                {
                    using (StringFormat string_format = new StringFormat())
                    {
                        string_format.Alignment = StringAlignment.Near;
                        string_format.LineAlignment = StringAlignment.Near;


                        // Make a CharacterRange for the string's characters.
                        List<CharacterRange> range_list =
                            new List<CharacterRange>();
                        for (int x = 0; x < tkn._length; x++)
                        {
                            range_list.Add(new CharacterRange(x, 1));
                        }
                        string_format.SetMeasurableCharacterRanges(
                            range_list.ToArray());

                        // Measure the string's character ranges.
                        try
                        {
                            Region[] regions = e.Graphics.MeasureCharacterRanges(
                                tkn._strToken, Font, e.ClipRectangle, string_format);
                            float width = 0;
                            for (int x = 0; x < regions.Length; x++)
                                width += regions[x].GetBounds(gr).Width;
                            e.Graphics.DrawString(tkn._strToken, Font, SystemBrushes.MenuText, posX, _rectContent.Top + (Font.Height + 2) * i, string_format);
                            posX += width;
                        }
                        catch (Exception x) { throw; }


                    }

                }
            }
        }
    }
}
public static class Tokenizer
{
    static char[] seps = { '=', ',', '(', ')' };
    static char[] integers = { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    static char[] hexNums = { 'A', 'B', 'C', 'D', 'E', 'F' };
    public static StringToken[] Tokenize(string data)
    {
        List<StringToken> _tokens = new List<StringToken>();
        int i = 0;
        while (i < data.Length)
        {
            StringToken str = new StringToken();
            str._index = i;
            if (seps.Contains(data[i]))
                _tokens.Add(new StringToken() { _strToken = data[i++].ToString(), _type = TokenType.String, _length = 1 });
            else
            {
                while (!seps.Contains(data[i]))
                {
                    str._strToken += data[i];
                    i++;
                    str._length++;
                }
                if (str._strToken.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                    str._type = TokenType.Integer;
                else if (integers.Contains(str._strToken[0]) && str._strToken.EndsWith("f"))
                    str._type = TokenType.FloatingPoint;
                else if (data[i] == '=')
                    str._type = TokenType.Keyword;
            }
            _tokens.Add(str);
        }
        return _tokens.ToArray();
    }
}

public struct StringToken
{
    public int _index;
    public int _length;
    public string _strToken;
    public TokenType _type;
}

public enum TokenType
{
    String,
    Keyword,
    Integer,
    FloatingPoint
}