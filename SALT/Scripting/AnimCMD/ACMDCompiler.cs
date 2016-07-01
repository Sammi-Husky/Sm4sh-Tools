using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SALT.Scripting.AnimCMD
{
    public class ACMDCompiler
    {
        private List<ACMDCommand> Commands { get; set; }

        /// <summary>
        /// Compile an array of code lines.
        /// </summary>
        /// <param name="lines">Array of plaintext code lines.</param>
        /// <returns></returns>
        public ACMDCommand[] Compile(string[] lines)
        {
            var tmpList = lines.ToList();
            tmpList.RemoveAll(x => string.IsNullOrEmpty(x));
            Commands.Clear();
            for (int i = 0; i < tmpList.Count; i++)
            {
                string lineText = tmpList[i].Trim();
                if (lineText.StartsWith("//"))
                    continue;

                ACMDCommand cmd = this.CompileSingleCommand(tmpList[i]);
                uint ident = cmd.Ident;


                int amt = 0;
                if ((amt = this.HandleSpecialCommands(i, ident, ref tmpList)) > 0)
                {
                    i += amt;
                    continue;
                }
                else
                {
                    Commands.Add(cmd);
                }
            }
            return Commands.ToArray();
        }
        /// <summary>
        /// Opens and compiles the contents of a plaintext script file.
        /// </summary>
        /// <param name="filepath">The path to the file to compile</param>
        /// <returns></returns>
        public ACMDCommand[] Compile(string filepath)
        {
            var text = File.ReadAllLines(filepath);
            return Compile(text.ToArray());
        }
        /// <summary>
        /// Compile a single ACMD command from plaintext.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private ACMDCommand CompileSingleCommand(string line)
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

        private int HandleSpecialCommands(int index, uint ident, ref List<string> lines)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return this.CompileConditional(index, ref lines);
                case 0x0EB375E3:
                    return this.CompileLoop(index, ref lines);
            }

            return 0;
        }

        private int CompileConditional(int startIndex, ref List<string> lines)
        {
            ACMDCommand cmd = this.CompileSingleCommand(lines[startIndex]);
            int i = startIndex;
            int len = 2;
            Commands.Add(cmd);
            while (lines[++i].Trim() != "}")
            {
                ACMDCommand tmp = this.CompileSingleCommand(lines[i]);
                len += tmp.Size / 4;
                if (this.IsCmdHandled(tmp.Ident))
                    i += this.HandleSpecialCommands(i, tmp.Ident, ref lines);
                else
                    Commands.Add(tmp);
            }

            Commands[Commands.IndexOf(cmd)].Parameters[0] = len;
            // Next line should be closing bracket, ignore and skip it
            return i - startIndex;
        }

        private int CompileLoop(int index, ref List<string> lines)
        {
            int i = index;
            Commands.Add(this.CompileSingleCommand(lines[i]));
            decimal len = 0;
            while (this.CompileSingleCommand(lines[++i]).Ident != 0x38A3EC78)
            {
                ACMDCommand tmp = this.CompileSingleCommand(lines[i]);
                len += (tmp.Size / 4);
                i += this.HandleSpecialCommands(i, tmp.Ident, ref lines);
                Commands.Add(tmp);
            }

            ACMDCommand endLoop = this.CompileSingleCommand(lines[i]);
            endLoop.Parameters[0] = len / -1;
            Commands.Add(endLoop);
            // Next line should be closing bracket, ignore and skip it
            return ++i - index;
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
    }
}
