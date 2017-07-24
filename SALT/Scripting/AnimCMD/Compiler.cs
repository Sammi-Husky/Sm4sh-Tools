using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SALT.Moveset.AnimCMD
{
    public static class ACMDCompiler
    {
        static ACMDCompiler()
        {
            Commands = new List<ACMDCommand>();
        }

        private static List<ACMDCommand> Commands { get; set; }

        /// <summary>
        /// Opens and compiles the contents of a plaintext script file.
        /// </summary>
        /// <param name="filepath">The path to the file to compile</param>
        /// <returns></returns>
        public static List<MoveDef> CompileFile(string filepath)
        {
            return Compile(File.ReadAllText(filepath));
        }

        private static List<MoveDef> Compile(string input)
        {
            MoveDef move = null;
            StringToken lastToken = new StringToken();
            string curLine = "", codeRegion = "";

            List<string> lines = new List<string>();
            List<MoveDef> movedefs = new List<MoveDef>();
            bool CodeBlock = false;
            int bracketScope = 0;

            foreach (var tok in Tokenizer.Tokenize(input))
            {
                if (tok.Token == "MoveDef")
                {
                    if (move != null)
                        movedefs.Add(move);

                    move = new MoveDef();
                }
                if (lastToken.Token == "MoveDef")
                {
                    move.AnimName = tok.Token;
                    goto end;
                }
                if (tok.Token == "Unlisted")
                {
                    move.Unlisted = true;
                    goto end;
                }
                else if (tok.TokType == TokenType.Bracket)
                {
                    if (tok.Token == "{")
                        bracketScope++;

                    // End of a code block, try and compile
                    if (CodeBlock && tok.TokType == TokenType.Bracket && bracketScope == 2)
                    {
                        var commands = ACMDCompiler.CompileCommands(lines.ToArray()).ToList();
                        if (commands.Count > 0)
                        {
                            move[codeRegion] = commands;
                        }

                        CodeBlock = false;
                        codeRegion = string.Empty;
                        lines.Clear();

                    }
                    else if (bracketScope > 2)
                        curLine += tok.Token;

                    // Marks the beginning of a code block, indicates that we should start
                    // adding tokens together to form a command string.
                    if (lastToken.Token == "Main" || lastToken.Token == "Effect" ||
                        lastToken.Token == "Expression" || lastToken.Token == "Sound")
                    {
                        CodeBlock = true;
                        codeRegion = lastToken.Token;
                    }

                    if (tok.Token == "}")
                        bracketScope--;
                }
                else if (CodeBlock && tok.Token != "\n" && tok.Token != "\r") // if this isn't a newline, add the token to the code line.
                {
                    curLine += tok.Token;
                    goto end;
                }
                else if (CodeBlock && tok.Token == "\n") // Add the completed code line to the list of lines for compilation
                {
                    if (string.IsNullOrEmpty(curLine))
                        goto end;

                    curLine += tok.Token;
                    lines.Add(curLine);
                    curLine = string.Empty;
                }

                end:
                if (tok.TokType != TokenType.Seperator)
                    lastToken = tok;
            }
            if (move != null)
                movedefs.Add(move);

            // clean up in case we compile more than one file
            Commands.Clear();
            return movedefs;
        }

        /// <summary>
        /// Compile a single ACMD command from plaintext.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static ACMDCommand CompileSingleCommand(string line)
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
        /// <summary>
        /// Compile an array of code lines.
        /// </summary>
        /// <param name="lines">Array of plaintext code lines.</param>
        /// <returns></returns>
        public static ACMDCommand[] CompileCommands(string[] lines)
        {
            var tmpList = lines.ToList().Select(x => x.Trim()).ToList();
            tmpList.RemoveAll(x => string.IsNullOrWhiteSpace(x));
            Commands.Clear();
            for (int i = 0; i < tmpList.Count; i++)
            {
                string lineText = tmpList[i].Trim();

                // Handle comments
                if (lineText.StartsWith("//"))
                    continue;

                if (lineText.StartsWith("/*"))
                {
                    while (tmpList[i].Trim() != "*/")
                    {
                        i++;
                    }
                    if (++i < tmpList.Count)
                        lineText = tmpList[i].Trim();
                    else
                        break;
                }

                ACMDCommand cmd = CompileSingleCommand(tmpList[i]);
                uint ident = cmd.Ident;

                if (HandleSpecialCommands(ref i, ident, ref tmpList) > 0)
                {
                    continue;
                }
                else
                {
                    Commands.Add(cmd);
                }
            }
            return Commands.ToArray();
        }

        private static int HandleSpecialCommands(ref int index, uint ident, ref List<string> lines)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return CompileConditional(ref index, ref lines);
                case 0x0EB375E3:
                    return CompileLoop(ref index, ref lines);
            }

            return 0;
        }

        private static int CompileConditional(ref int Index, ref List<string> lines)
        {
            ACMDCommand cmd = CompileSingleCommand(lines[Index]);
            int len = 2;
            Commands.Add(cmd);

            while (lines[++Index].Trim() != "}")
            {
                ACMDCommand tmp = CompileSingleCommand(lines[Index]);
                if (IsCmdHandled(tmp.Ident))
                {
                    len += HandleSpecialCommands(ref Index, tmp.Ident, ref lines);
                }
                else
                {
                    len += tmp.Size / 4;
                    Commands.Add(tmp);
                }
            }

            if (lines[Index + 1] != "}")
            {
                if (CompileSingleCommand(lines[Index + 1]).Ident == 0x895B9275 && cmd.Ident == 0xA5BD4F32)
                {
                    ACMDCommand tmp = CompileSingleCommand(lines[++Index]);
                    len += tmp.Size / 4;
                    Commands[Commands.IndexOf(cmd)].Parameters[0] = len;
                    len -= tmp.Size / 4;
                    len += HandleSpecialCommands(ref Index, tmp.Ident, ref lines);
                }
            }
            else
            {
                Commands[Commands.IndexOf(cmd)].Parameters[0] = len;
            }

            // Next line should be closing bracket, ignore and skip it
            return len;
        }

        private static int CompileLoop(ref int Index, ref List<string> lines)
        {
            Commands.Add(CompileSingleCommand(lines[Index]));

            decimal len = 0;
            while (CompileSingleCommand(lines[++Index]).Ident != 0x38A3EC78)
            {
                ACMDCommand tmp = CompileSingleCommand(lines[Index]);
                if (IsCmdHandled(tmp.Ident))
                {
                    len += HandleSpecialCommands(ref Index, tmp.Ident, ref lines);
                }
                else
                {
                    len += (tmp.Size / 4);
                    Commands.Add(tmp);
                }
            }

            ACMDCommand endLoop = CompileSingleCommand(lines[Index]);

            endLoop.Parameters[0] = len / -1;
            Commands.Add(endLoop);

            if (lines[Index + 1].Trim() == "}")
                Index++;

            // Next line should be closing bracket, ignore and skip it
            return (int)len+4;
        }

        private static bool IsCmdHandled(uint ident)
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

    public static class ACMDDecompiler
    {
        public static List<string> lines = new List<string>();
        private static int Scope = 0;
        private static ACMDScript Script;

        public static string DecompileCommands(ACMDScript script)
        {
            Script = script;
            for (int i = 0; i < Script.Count; i++)
            {
                var cmd = Script.Commands[i];
                var cmdIdent = cmd.Ident;
                var cmdline = cmd.ToString();
                var it = HandleCommand(cmdIdent, i);
                i += it; //This is to avoid repeating lines contained in the loop after the loop condition is closed
                if (it == 0)
                {
                    lines.Add(cmd.ToString() + '\n');
                }
            }
            var text = string.Join("", lines);

            lines.Clear();
            Scope = 0;
            Script = null;

            return text;
        }
        private static int HandleCommand(uint ident, int index)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    //return HandleConditional(index);
                    return 0;
                case 0x0EB375E3:
                    return Handleloop(index);
            }
            return 0;
        }
        private static int Handleloop(int index)
        {
            int handledCommands = 0;
            var cmd = Script[index];

            string tabs = "";
            for (int i = 0; i < Scope; i++)
                tabs = tabs.Insert(0, "\t");

            lines.Add($"{tabs}{cmd.ToString()}\n");
            lines.Add($"{tabs}{{\n");
            tabs = tabs.Insert(0, "\t");
            Scope++;
            while ((cmd = Script.Commands[++index]).Ident != 0x38A3EC78)
            {
                int amt = HandleCommand(Script[index].Ident, index);
                handledCommands += amt + 1;
                if (amt == 0)
                {
                    lines.Add($"{tabs}{cmd.ToString()}\n");
                }
            }
            Scope--;
            lines.Add($"{tabs}{cmd.ToString()}\n");
            tabs = tabs.Remove(0, 1);
            lines.Add($"{tabs}}}\n");
            return handledCommands + 1;
        }

        //private static int HandleConditional(int index)
        //{
        //    int handledCommands = 0;
        //    var cmd = Script[index];
        //    int size = 2;
        //    int blocksize = (int)cmd.Parameters[0];

        //    string tabs = "";
        //    for (int i = 0; i < Scope; i++)
        //        tabs = tabs.Insert(0, "\t");
        //    lines.Add($"{tabs}{cmd.ToString()}\n");
        //    lines.Add($"{tabs}{{\n");
        //    tabs = tabs.Insert(0, "\t");
        //    Scope++;
        //    while ((size += Script[++index].Size) != blocksize)
        //    {
        //        int amt = HandleCommand(Script[index].Ident, index);
        //        size += amt;
        //        handledCommands += amt;
        //        if (amt == 0)
        //        {
        //            lines.Add($"{tabs}{cmd.ToString()}\n");
        //        }
        //    }
        //    Scope--;
        //    tabs = tabs.Remove(0, 1);
        //    lines.Add($"{tabs}}}\n");
        //    return handledCommands + 1;
        //}
    }

    public class MoveDef
    {
        public string AnimName { get; set; }
        public uint CRC
        {
            get
            {
                uint hash = 0;
                if (AnimName.StartsWith("0x"))
                    hash = UInt32.Parse(AnimName.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    hash = System.Security.Cryptography.Crc32.Compute(AnimName.ToLower());
                return hash;
            }
        }

        public bool Unlisted { get; set; }

        public List<ACMDCommand> Game { get; set; }
        public List<ACMDCommand> Effect { get; set; }
        public List<ACMDCommand> Sound { get; set; }
        public List<ACMDCommand> Expression { get; set; }

        public List<ACMDCommand> this[string id]
        {
            get
            {
                switch (id)
                {
                    case "Main":
                        return Game;
                    case "Effect":
                        return Effect;
                    case "Sound":
                        return Sound;
                    case "Expression":
                        return Expression;
                    default:
                        throw new Exception($"{id} is not a valid event list identifier");
                }
            }
            set
            {
                switch (id)
                {
                    case "Main":
                        Game = value;
                        break;
                    case "Effect":
                        Effect = value;
                        break;
                    case "Sound":
                        Sound = value;
                        break;
                    case "Expression":
                        Expression = value;
                        break;
                    default:
                        throw new Exception($"{id} is not a valid event list identifier");
                }
            }
        }
        public List<ACMDCommand> this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Game;
                    case 1:
                        return Effect;
                    case 2:
                        return Sound;
                    case 3:
                        return Expression;
                    default:
                        throw new Exception($"Could not get event list with index of {index}");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Game = value;
                        break;
                    case 1:
                        Effect = value;
                        break;
                    case 2:
                        Sound = value;
                        break;
                    case 3:
                        Expression = value;
                        break;
                    default:
                        throw new Exception($"Could not set event list with index of {index}");
                }
            }
        }
    }

    public static class Tokenizer
    {
        static char[] seps = { '=', ',', '(', ')', ' ', '\n', '\r', '\t', ':' };
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
                        str.TokType = TokenType.Syntax;
                    else if (str.Token == "{" || str.Token == "}")
                        str.TokType = TokenType.Bracket;
                    else
                        str.TokType = TokenType.String;

                    _tokens.Add(str);
                }
            }
            return _tokens.ToArray();
        }
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
    }

    public enum TokenType
    {
        String,
        Syntax,
        Integer,
        FloatingPoint,
        Seperator,
        Bracket,
        NewLine,
    }
}
