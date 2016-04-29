using FastColoredTextBoxNS;
using SALT.Scripting.AnimCMD;
using Sm4shCommand.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sm4shCommand
{
    public abstract class ITS_EDITOR : FastColoredTextBox
    {
        TextStyle keywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        TextStyle HexStyle = new TextStyle(Brushes.DarkCyan, null, FontStyle.Regular);
        TextStyle DecStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);
        public AutocompleteMenu AutocompleteMenu { get; set; }

        public ITS_EDITOR()
        {
            this.TextChanged += NewBox_TextChanged;
            this.AutoCompleteBrackets = true;
            this.AutoIndent = true;
        }
        private void NewBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //clear previous highlighting
            e.ChangedRange.ClearStyle(StyleIndex.All);
            //highlight tags
            e.ChangedRange.SetStyle(keywordStyle, @"(?<=[\(,])+[^=)]+(?==)\b");
            e.ChangedRange.SetStyle(HexStyle, @"0x[^\),]+\b");
            e.ChangedRange.SetStyle(DecStyle, @"\b(?:[0-9]*\\.)?[0-9]+\b");
        }
    }
    public class ACMD_EDITOR : ITS_EDITOR
    {
        public ACMD_EDITOR(ACMDScript list)
        {
            this.AutocompleteMenu = new AutocompleteMenu(this) { AppearInterval = 1 };
            this.AutocompleteMenu.Items.SetAutocompleteItems(ACMD_INFO.CMD_NAMES.Select(x => x.Value).ToArray());
            this.AutoCompleteBrackets = true;
            this.AutoIndent = true;

            Script = list;
            Deserialize();
        }
        public ACMDScript Script { get; set; }
        private List<string> tmplines = new List<string>();

        public void Deserialize()
        {
            this.Text = string.Empty;
            for (int i = 0; i < Script.Count; i++)
            {
                int amt = 0;
                if ((amt = DeserializeCommand(i, Script[i].CRC)) > 0)
                    i += amt;

                if (i < Script.Count)
                    tmplines.Add(Script[i].ToString());
            }
            if (Script.Empty)
                tmplines.Add("// Empty list");

            DoFormat();
            this.Text = string.Join(Environment.NewLine, tmplines);
        }
        public void Serialize()
        {
            Serialize(Lines.ToList());
        }
        public void Serialize(List<string> lines)
        {
            lines.RemoveAll(x => string.IsNullOrEmpty(x));
            Script.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                string lineText = lines[i].Trim();
                if (lineText.StartsWith("//"))
                    continue;

                ACMDCommand cmd = ParseCMD(lines[i]);
                uint ident = cmd.CRC;


                int amt = 0;
                if ((amt = SerializeCommands(i, ident)) > 0)
                {
                    i += amt;
                    continue;
                }
                else
                    Script.Add(cmd);
            }
        }

        private int DeserializeCommand(int index, uint ident)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return DeserializeConditional(index);
                case 0x0EB375E3:
                    return DeserializeLoop(index);
            }
            return 0;
        }
        private int DeserializeConditional(int startIndex)
        {
            int i = startIndex;

            string str = Script[startIndex].ToString();
            int len = (int)Script[startIndex].Parameters[0] - 2;
            tmplines.Add($"{str}{{");
            int count = 1;
            i++;

            while (len > 0)
            {
                len -= Script[i].CalcSize() / 4;

                if (IsCmdHandled(Script[i].CRC))
                    break;
                else
                {
                    tmplines.Add('\t' + Script[i].ToString());
                    i++;
                    count++;
                }
            }
            if (IsCmdHandled(Script[i].CRC))
                i += (count += DeserializeCommand(i, Script[i].CRC));
            tmplines.Add("}");
            return count;
        }
        private int DeserializeLoop(int startIndex)
        {
            int i = startIndex;

            string str = Script[startIndex].ToString();
            int len = 0;
            str += '{';
            tmplines.Add(str);
            while (Script[++i].CRC != 0x38A3EC78)
            {
                len += Script[i].CalcSize() / 4;
                i += DeserializeCommand(i, Script[i].CRC);
                tmplines.Add('\t' + Script[i].ToString());
            }
            tmplines.Add('\t' + Script[i].ToString());
            tmplines.Add("}");
            return ++i - startIndex;
        }

        private int SerializeCommands(int index, uint ident)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return SerializeConditional(index);
                case 0x0EB375E3:
                    return SerializeLoop(index);
            }
            return 0;
        }
        private int SerializeConditional(int startIndex)
        {
            ACMDCommand cmd = ParseCMD(Lines[startIndex]);
            int i = startIndex;
            int len = 2;
            Script.Add(cmd);
            while (Lines[++i].Trim() != "}")
            {
                ACMDCommand tmp = ParseCMD(Lines[i]);
                len += tmp.CalcSize() / 4;
                if (IsCmdHandled(tmp.CRC))
                    i += SerializeCommands(i, tmp.CRC);
                else
                    Script.Add(tmp);
            }
            Script[Script.IndexOf(cmd)].Parameters[0] = len;
            // Next line should be closing bracket, ignore and skip it
            return i - startIndex;
        }
        private int SerializeLoop(int index)
        {
            int i = index;
            Script.Add(ParseCMD(Lines[i]));
            decimal len = 0;
            while (ParseCMD(Lines[++i]).CRC != 0x38A3EC78)
            {
                ACMDCommand tmp = ParseCMD(Lines[i]);
                len += (tmp.CalcSize() / 4);
                i += SerializeCommands(i, tmp.CRC);
                Script.Add(tmp);
            }
            ACMDCommand endLoop = ParseCMD(Lines[i]);
            endLoop.Parameters[0] = len / -1;
            Script.Add(endLoop);
            // Next line should be closing bracket, ignore and skip it
            return ++i - index;
        }

        private ACMDCommand ParseCMD(string line)
        {
            string s = line.TrimStart();
            s = s.Substring(0, s.IndexOf(')'));
            var name = s.Substring(0, s.IndexOf('('));
            var parameters =
                s.Substring(s.IndexOf('(')).TrimEnd(')').Split(',').Select(x =>
                x.Remove(0, x.IndexOf('=') + 1)).ToArray();

            var crc = ACMD_INFO.CMD_NAMES.Single(x => x.Value == name).Key;
            ACMDCommand cmd = new ACMDCommand(crc);
            for (int i = 0; i < cmd.ParamSpecifiers.Length; i++)
            {
                switch (cmd.ParamSpecifiers[i])
                {
                    case 0:
                        cmd.Parameters.Add(int.Parse(parameters[i].Substring(2), System.Globalization.NumberStyles.HexNumber));
                        break;
                    case 1:
                        cmd.Parameters.Add(float.Parse(parameters[i]));
                        break;
                    case 2:
                        cmd.Parameters.Add(decimal.Parse(parameters[i]));
                        break;
                }
            }
            return cmd;
        }
        private bool IsCmdHandled(uint ident)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return true;
                case 0x0EB375E3:
                    return true;
            }
            return false;
        }
        private void DoFormat()
        {
            int curindent = 0;
            for (int i = 0; i < tmplines.Count; i++)
            {
                if (tmplines[i].StartsWith("//"))
                    continue;

                if (tmplines[i].EndsWith("}"))
                    curindent--;
                string tmp = tmplines[i].TrimStart();
                for (int x = 0; x < curindent; x++)
                    tmp = tmp.Insert(0, "    ");
                tmplines[i] = tmp;
                if (tmplines[i].EndsWith("{"))
                    curindent++;
            }
        }

        public void ApplyChanges()
        {
            var tmp = Lines.ToList();
            tmp.RemoveAll(x => string.IsNullOrEmpty(x));
            Script.Clear();
            for (int i = 0; i < Lines.Count; i++)
            {
                string lineText = tmp[i].Trim();
                if (lineText.StartsWith("//"))
                    continue;

                ACMDCommand cmd = ParseCMD(tmp[i]);
                uint ident = cmd.CRC;


                int amt = 0;
                if ((amt = SerializeCommands(i, ident)) > 0)
                {
                    i += amt;
                    continue;
                }
                else
                    Script.Add(cmd);
            }
        }
    }
}