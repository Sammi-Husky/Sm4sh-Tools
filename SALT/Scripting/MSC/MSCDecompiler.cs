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
            STACK = new Stack<StackParam>();
            INDENT_STACK = new List<int>();
            ASSIGNMENTS = new Stack<MSCCommand>();
        }

        public MSCFile File { get; set; }

        public Stack<StackParam> STACK { get; set; }
        public List<int> INDENT_STACK { get; set; }

        public Stack<MSCCommand> ASSIGNMENTS = null;

        //public string DecompileScript(MSCScript script)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (MSCCommand cmd in script.Commands)
        //    {

        //        while (INDENT_STACK.Contains(cmd.FileOffset - 0x30))
        //        {
        //            INDENT_STACK.Remove(cmd.FileOffset - 0x30);
        //            foreach (int i in INDENT_STACK)
        //                sb.Append("   ");
        //            sb.Append("}\n");
        //        }


        //        switch (cmd.Ident)
        //        {
        //            // Handle any commands that push onto the stack
        //            case 0x0A:
        //                STACK.Push(new StackParam(SPType.Integer, cmd.Parameters[0]));
        //                break;
        //            case 0x0B:
        //                var variable = new spVariable((byte)cmd.Parameters[0] > 0, (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
        //                STACK.Push(new StackParam(SPType.Variable, variable));
        //                break;
        //            case 0x0D:
        //                STACK.Push(new StackParam(SPType.Short, (short)cmd.Parameters[0]));
        //                break;
        //            case 0x26:
        //                STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.Not_equal));
        //                break;
        //            case 0x27:
        //                STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.Equal));
        //                break;
        //            case 0x2A:
        //                STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.Greater_or_equal));
        //                break;
        //            case 0x2B:
        //                STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.TrueFalse));
        //                break;
        //            case 0x2D:
        //                STACK.Push(new StackParam(SPType.Function, (byte)cmd.Parameters[1]));
        //                goto default;
        //            default:
        //                // If the command is passed to the next add it to the assignment stack
        //                if (cmd.Returns)
        //                {
        //                    ASSIGNMENTS.Push(cmd);
        //                    continue;
        //                }
        //                else
        //                {
        //                    sb.Append(DecompileCMD(cmd)/*.TrimEnd() + $" \t// 0x{cmd.FileOffset-0x30:X}\n"*/);
        //                }
        //                break;
        //        }
        //    }
        //    return sb.ToString();
        //}

        public string DecompileScript(MSCScript script)
        {
            StringBuilder sb = new StringBuilder();
            foreach (MSCCommand cmd in script.Commands)
            {

                while (INDENT_STACK.Contains(cmd.FileOffset - 0x30))
                {
                    INDENT_STACK.Remove(cmd.FileOffset - 0x30);
                    foreach (int i in INDENT_STACK)
                        sb.Append("   ");

                    sb.Append("}\n");
                }

                // If the command is passed to the next add it to the assignment stack
                if (cmd.Returns)
                {
                    ASSIGNMENTS.Push(cmd);
                    continue;
                }
                else
                {
                    sb.Append(DecompileCMD(cmd)/*.TrimEnd() + $" \t// 0x{cmd.FileOffset-0x30:X}\n"*/);
                }
            }
            return sb.ToString();
        }



        private string DecompileCMD(MSCCommand cmd)
        {
            var sb = new StringBuilder();
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
                case 0x2B:
                    sb.Append(Decompile_2B(cmd));
                    break;
                case 0x2C:
                    sb.Append(Decompile_2C(cmd));
                    break;
                case 0x2D:
                    sb.Append(Decompile_2D(cmd));
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
                    sb.Append(cmd.ToString() + Environment.NewLine);
                    break;
            }
            return sb.ToString();
        }

        private string Decompile_06(MSCCommand cmd)
        {
            return $"return_06 {DecompileCMD(ASSIGNMENTS.Pop()).Trim()}\n";
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
            if (STACK.Count > 0)
                return $"unk_13({STACK.Pop().Value})\n";
            else
                return "unk_13()\n";
        }

        private string Decompile_1C(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);

            var arg = ASSIGNMENTS.Pop();
            if (arg.Ident == 0x1C)
                return $"{text} = 0x{arg.Parameters[0]:X}\n";
            else
                return $"{text} = {DecompileCMD(arg)}\n";
        } // assign_var

        //============== Comparisons ===============//
        private string Decompile_26(MSCCommand cmd)
        {
            var arg1 = ASSIGNMENTS.Pop();
            var arg2 = ASSIGNMENTS.Pop();

            return $"{DecompileCMD(arg2)} != {DecompileCMD(arg1)}";
        } // equals
        private string Decompile_27(MSCCommand cmd)
        {
            var arg1 = ASSIGNMENTS.Pop();
            var arg2 = ASSIGNMENTS.Pop();

            return $"{DecompileCMD(arg2)} == {DecompileCMD(arg1)}";
        } // not_equals
        private string Decompile_2B(MSCCommand cmd)
        {
            var arg = ASSIGNMENTS.Pop();
            return $"{DecompileCMD(arg)}";
        } // true

        private string Decompile_2C(MSCCommand cmd)
        {
            var str = "printf(";
            var parameters = new List<string>();

            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                var arg = ASSIGNMENTS.Pop();
                switch (arg.Ident)
                {
                    case 0x1C:
                        var variable = FormatVariable((byte)arg.Parameters[0], (byte)arg.Parameters[1], (byte)arg.Parameters[2]);
                        parameters.Add(variable);
                        break;
                    case 0x0A:
                        parameters.Add($"\"{File.Strings[(int)arg.Parameters[0]]}\"");
                        break;
                    case 0x0D:
                        parameters.Add($"\"{File.Strings[(short)arg.Parameters[0]]}\"");
                        break;
                }
            }


            return str + $"{string.Join(", ", parameters)})\n";
        } // printf
        private string Decompile_2D(MSCCommand cmd)
        {

            var parameters = new List<string>();
            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                parameters.Add(DecompileCMD(ASSIGNMENTS.Pop()));
            }
            return $"func_{cmd.Parameters[1]:X}({string.Join(", ", parameters)})";
        } // call_func
        private string Decompile_31(MSCCommand cmd)
        {
            var arg = ASSIGNMENTS.Pop();
            var str = $"func_{File.Offsets.IndexOf((uint)(int)arg.Parameters[0]):X}";

            var parameters = new List<MSCCommand>();
            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                parameters.Add(ASSIGNMENTS.Pop());
            }
            var pStr = $"({string.Join(", ", parameters)})\n";

            return str + pStr;
        } // call_func_by_id

        //============== Flow Control ==============//
        private string Decompile_34(MSCCommand cmd)
        {
            var parameters = new List<string>();
            parameters.Add(DecompileCMD(ASSIGNMENTS.Pop()));

            var str = $"if({string.Join(" ", parameters)})\n{DoIndent("{")}\n";
            INDENT_STACK.Add((int)cmd.Parameters[0]);
            return str;
        } // if
        private string Decompile_36(MSCCommand cmd)
        {
            INDENT_STACK.Add((int)cmd.Parameters[0]);
            return $"else\n{DoIndent("{")}\n";
        } // else 
        private string Decompile_41(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            var arg = ASSIGNMENTS.Pop();

            if (arg.Ident == 0x41)
                return $"{text} = {DecompileCMD(arg)}";
            else
                return $"{text} = 0x{arg.Parameters[0]:X}\n";
        }

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

    public struct StackParam
    {
        public StackParam(SPType type, object val)
        {
            Type = type;
            Value = val;
        }
        public SPType Type;
        public object Value;
    }
    public struct spVariable
    {
        public spVariable(bool global, int unk, int id)
        {
            Global = global;
            Unk = (byte)unk;
            ID = (byte)id;
        }
        public bool Global;
        public byte Unk;
        public byte ID;

        public override string ToString()
        {
            return $"{(Global ? "global" : "local")}Var{ID}";
        }
    }

    public enum SPType
    {
        Variable,
        Integer,
        Short,
        Byte,

        ComparisonType,
        Function
    }
    public enum ComparisonType
    {
        Greater,
        Greater_or_equal,
        Less,
        Less_or_equal,
        Equal,
        Not_equal,
        TrueFalse
    }
}
