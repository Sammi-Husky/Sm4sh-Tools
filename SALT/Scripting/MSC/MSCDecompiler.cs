using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Scripting.MSC
{
    public class MSCDecompiler
    {
        public MSCDecompiler(MSCFile file)
        {
            File = file;
            INDENT_STACK = new List<int>();
            COMMANDS = new Stack<MSCCommand>();
        }

        public MSCFile File { get; set; }
        public MSCCommandManager Manager { get; set; }

        public List<int> INDENT_STACK { get; set; }
        public Stack<MSCCommand> COMMANDS = null;

        public string Decompile(MSCScript script)
        {
            StringBuilder sb = new StringBuilder();
            Manager = new MSCCommandManager(script);

            while (!Manager.End)
            {
                MSCCommand cmd = Manager.Next();

                // Do ending brackets
                while (INDENT_STACK.Contains(cmd.FileOffset - 0x30))
                {
                    INDENT_STACK.Remove(cmd.FileOffset - 0x30);
                    foreach (int off in INDENT_STACK)
                        sb.Append("   ");

                    sb.Append("}\n");
                }

                // If the command is passed to the next add it to the assignment stack
                if (Manager.Position != script.Count &&
                    cmd.Ident == 0x0A && (Manager.PeekNext().Ident == 0x36 || INDENT_STACK.Contains(Manager.PeekNext().FileOffset - 0x30)))
                {
                    sb.Append(DoIndent(cmd.ToString()) + "\n");
                    continue;

                }
                else if (cmd.Returns)
                {
                    COMMANDS.Push(cmd);
                    continue;
                }

                sb.Append(DecompileCMD(cmd)/*.TrimEnd() + $" \t// 0x{cmd.FileOffset - 0x30:X}*/+ "\n");

            }
            return sb.ToString();
        }
        //private List<MSCCommand> Analyze(MSCScript script)
        //{

        //}

        private string DecompileCMD(MSCCommand cmd)
        {
            var sb = new StringBuilder();

            // Do current indent
            if (!cmd.Returns)
            {
                foreach (int i in INDENT_STACK)
                {
                    sb.Append("   ");
                }
            }

            switch (cmd.Ident)
            {
                case 0x06:
                    sb.Append(Decompile_06(cmd));
                    break;
                case 0x0A:
                    sb.Append(Decompile_0A(cmd));
                    break;
                case 0x0B:
                    sb.Append(Decompile_0B(cmd));
                    break;
                case 0x0D:
                    sb.Append(Decompile_0D(cmd));
                    break;
                case 0x13:
                    sb.Append(Decompile_13(cmd));
                    break;
                case 0x1C:
                    sb.Append(Decompile_1C(cmd));
                    break;
                case 0x26:
                    sb.Append(Decompile_26(cmd));
                    break;
                case 0x2A:
                    sb.Append(Decompile_2A(cmd));
                    break;
                case 0x2B:
                    sb.Append(Decompile_2B(cmd));
                    break;
                case 0x2C:
                    sb.Append(Decompile_2C(cmd));
                    break;
                case 0x2D:
                    sb.Append(Decompile_2D(cmd));
                    break;
                case 0x2E:
                    sb.Append(Decompile_2E(cmd));
                    break;
                case 0x2F:
                    sb.Append(Decompile_2F(cmd));
                    break;
                case 0x31:
                    sb.Append(Decompile_31(cmd));
                    break;
                case 0x34:
                    sb.Append(Decompile_34(cmd));
                    break;
                case 0x36:
                    sb.Append(Decompile_36(cmd));
                    break;
                case 0x41:
                    sb.Append(Decompile_41(cmd));
                    break;
                default:
                    sb.Append(cmd.ToString());
                    break;
            }
            return sb.ToString();
        }


        #region CMD Decompilers
        private string Decompile_06(MSCCommand cmd)
        {
            return $"return_06 {DecompileCMD(COMMANDS.Pop()).Trim()}";
        } // return_6

        //=========== Stack Manipulation ===========//
        private string Decompile_0A(MSCCommand cmd)
        {
            if (cmd.Returns)
                return $"0x{cmd.Parameters[0]:X}";
            else
                return cmd.ToString();
        } // pushInt
        private string Decompile_0B(MSCCommand cmd)
        {
            return FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
        } // pushVar
        private string Decompile_0D(MSCCommand cmd)
        {
            return Decompile_0A(cmd);
        } // pushShort

        private string Decompile_13(MSCCommand cmd)
        {
            if (COMMANDS.Count > 0)
                return $"unk_13({DecompileCMD(COMMANDS.Pop())})";
            else
                return "unk_13()";
        }

        private string Decompile_1C(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);

            var arg = COMMANDS.Pop();
            if (arg.Ident == 0x1C)
                return $"{text} = 0x{arg.Parameters[0]:X}";
            else
                return $"{text} = {DecompileCMD(arg)}";
        } // assign_var

        //============== Comparisons ===============//
        private string Decompile_26(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} != {DecompileCMD(arg1)}";
        } // equals
        private string Decompile_27(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} == {DecompileCMD(arg1)}";
        } // not_equals
        private string Decompile_2A(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} >= {DecompileCMD(arg1)}";
        } // greater_or_equal
        private string Decompile_2B(MSCCommand cmd)
        {
            var arg = COMMANDS.Pop();
            return $"{DecompileCMD(arg)}";
        } // true

        //============== Functions? ================//
        private string Decompile_2C(MSCCommand cmd)
        {
            var str = "printf(";
            var parameters = new List<string>();

            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                var arg = COMMANDS.Pop();
                switch (arg.Ident)
                {
                    case 0x0A:
                        parameters.Add($"\"{File.Strings[(int)arg.Parameters[0]]}\"");
                        break;
                    case 0x0D:
                        parameters.Add($"\"{File.Strings[(short)arg.Parameters[0]]}\"");
                        break;
                    case 0x0B:
                    case 0x1C:
                        var variable = FormatVariable((byte)arg.Parameters[0], (byte)arg.Parameters[1], (byte)arg.Parameters[2]);
                        parameters.Add(variable);
                        break;
                    default:
                        parameters.Add(DecompileCMD(arg));
                        break;
                }
            }

            parameters.Reverse();
            return str + $"{string.Join(", ", parameters)})";
        } // printf
        private string Decompile_2D(MSCCommand cmd)
        {
            var parameters = new List<string>();
            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                parameters.Add(DecompileCMD(COMMANDS.Pop()));
            }
            return $"global_{cmd.Parameters[1]:X}({string.Join(", ", parameters)})";
        } // call_func
        private string Decompile_2E(MSCCommand cmd)
        {

            var str = $"unk_2E\n{DoIndent("{")}";
            INDENT_STACK.Add((int)cmd.Parameters[0]);
            return str;
        } // Try? No parameter code block
        private string Decompile_2F(MSCCommand cmd)
        {
            //return Decompile_31(cmd);
            var parameters = new List<string>();
            var arg1 = COMMANDS.Pop();
            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                parameters.Add(DecompileCMD(COMMANDS.Pop()));
            }
            return $"func_{DecompileCMD(arg1)}({string.Join(", ", parameters)})";
        } // same as 2D but always comes after 2F?
        private string Decompile_31(MSCCommand cmd)
        {
            var arg = COMMANDS.Pop();
            var str = $"func_{File.Offsets.IndexOf((uint)(int)arg.Parameters[0]):X}";

            var parameters = new List<MSCCommand>();
            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                parameters.Add(COMMANDS.Pop());
            }
            var pStr = $"({string.Join(", ", parameters)})";

            return str + pStr;
        } // call_func_by_id

        //============== Flow Control ==============//

        private string Decompile_34(MSCCommand cmd)
        {
            var str = "";
            if (COMMANDS.Count == 0)
                str = $"if(Stack[0])\n{DoIndent("{")}";
            else
            {
                var arg = COMMANDS.Peek();
                if (arg.Ident != 0x0A)
                    str = $"if({DecompileCMD(COMMANDS.Pop())})\n{DoIndent("{")}";
            }

            INDENT_STACK.Add((int)cmd.Parameters[0]);
            return str;
        } // if
        private string Decompile_36(MSCCommand cmd)
        {
            var str = $"{DoIndent("}\n")}";

            if (INDENT_STACK.Contains(cmd.FileOffset - 0x30))
                INDENT_STACK.Remove(cmd.FileOffset - 0x30);

            str += $"{DoIndent("else\n")}{DoIndent("{")}";
            INDENT_STACK.Add((int)cmd.Parameters[0]);
            return str;
        } // else
        private string Decompile_41(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            var arg = COMMANDS.Pop();


            return $"{text} = {DecompileCMD(arg)}";
        }
        #endregion

        #region CMD Analyzers
        #endregion

        private string FormatVariable(byte scope, byte unk, byte id)
        {
            return $"{(scope > 0 ? "Global" : "Local")}Var{id}";
        }
        private string DoIndent(string source)
        {
            var str = source;
            foreach (int i in INDENT_STACK)
                str = str.Insert(0, "   ");
            return str;
        }
    }
    public class MSCCommandManager
    {
        public MSCCommandManager(MSCScript script)
        {
            Position = 0;
            End = false;
            Script = script;
        }

        public int Position { get; set; }
        public bool End { get; set; }
        public MSCScript Script { get; set; }

        public MSCCommand Next()
        {
            if (Position >= Script.Count)
                throw new IndexOutOfRangeException();

            var cmd = (MSCCommand)Script[Position++];
            End = (Position == Script.Count);
            return cmd;
        }
        public MSCCommand Previous()
        {
            if (Position <= 0)
                throw new IndexOutOfRangeException();

            var cmd = (MSCCommand)Script[Position--];
            End = (Position == Script.Count);
            return cmd;
        }
        public MSCCommand PeekNext()
        {
            if (Position >= Script.Count)
                throw new IndexOutOfRangeException();

            var cmd = (MSCCommand)Script[Position];
            return cmd;
        }
        public MSCCommand PeekPrevious()
        {
            if (Position <= 0)
                throw new IndexOutOfRangeException();

            var cmd = (MSCCommand)Script[Position - 2];
            return cmd;
        }


        public void Rewind()
        {
            Position = 0;
        }
    }
}
