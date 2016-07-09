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
            INDENT_STACK = new Stack<int>();
        }

        public MSCFile File { get; set; }

        public Stack<StackParam> STACK { get; set; }
        public Stack<int> INDENT_STACK { get; set; }

        public string DecompileScript(MSCScript script)
        {
            StringBuilder sb = new StringBuilder();
            foreach (MSCCommand cmd in script.Commands)
            {
                switch (cmd.Ident)
                {
                    case 0x0A:
                    case 0x8A:
                        STACK.Push(new StackParam(SPType.Integer, cmd.Parameters[0]));
                        break;
                    case 0x0B:
                    case 0x8B:
                        var variable = new spVariable((byte)cmd.Parameters[0] > 0, (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
                        STACK.Push(new StackParam(SPType.Variable, variable));
                        break;
                    case 0x0D:
                    case 0x8D:
                        STACK.Push(new StackParam(SPType.Short, (short)cmd.Parameters[0]));
                        break;
                    case 0x26:
                    case 0xA6:
                        STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.Not_equal));
                        break;
                    case 0x27:
                    case 0xA7:
                        STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.Equal));
                        break;
                    case 0x2A:
                    case 0xAA:
                        STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.Greater_or_equal));
                        break;
                    case 0x2B:
                    case 0xAB:
                        STACK.Push(new StackParam(SPType.ComparisonType, ComparisonType.TrueFalse));
                        break;
                    case 0xAD:
                        STACK.Push(new StackParam(SPType.Function, (byte)cmd.Parameters[1]));
                        break;
                    default:
                        sb.Append(DecompileCMD(cmd));
                        break;
                }
                loop:
                if (INDENT_STACK.Contains(cmd.FileOffset-0x30))
                {
                    INDENT_STACK.Pop();
                    foreach (int i in INDENT_STACK)
                        sb.Append("   ");
                    sb.Append("}\n");
                    goto loop;
                }
            }
            return sb.ToString();
        }

        private string DecompileCMD(MSCCommand cmd)
        {
            var sb = new StringBuilder();
            foreach (int i in INDENT_STACK)
                sb.Append("   ");

            switch (cmd.Ident)
            {
                case 0x1C:
                    sb.Append(Decompile_1C(cmd));
                    break;
                case 0x2C:
                    sb.Append(Decompile_2C(cmd));
                    break;
                case 0x34:
                    sb.Append(Decompile_34(cmd));
                    break;
                case 0x2D:
                    sb.Append($"func_{cmd.Parameters[1]:X}");
                    break;
                default:
                    sb.Append(cmd.ToString() + Environment.NewLine);
                    break;
            }
            return sb.ToString();
        }
        private string Decompile_2C(MSCCommand cmd)
        {
            var str = "printf(";
            var parameters = new List<string>();

            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                var sp = STACK.Pop();
                switch (sp.Type)
                {
                    case SPType.Variable:
                        var variable = ((spVariable)sp.Value);
                        parameters.Add($"{(variable.Global ? "global" : "local")}Var{((spVariable)sp.Value).ID}");
                        break;
                    case SPType.Integer:
                        parameters.Add($"\"{File.Strings[(int)sp.Value]}\"");
                        break;
                    case SPType.Short:
                        parameters.Add($"\"{File.Strings[(short)sp.Value]}\"");
                        break;
                }
            }


            return str + $"{string.Join(", ", parameters)})\n";
        }
        private string Decompile_34(MSCCommand cmd)
        {
            var parameters = new List<string>();
            int paramCount = 0;
            string cmpSign = "";

            INDENT_STACK.Push((int)cmd.Parameters[0]);
            switch ((ComparisonType)STACK.Pop().Value)
            {
                case ComparisonType.Equal:
                    cmpSign = "==";
                    paramCount = 2;
                    break;
                case ComparisonType.Not_equal:
                    cmpSign = "!=";
                    paramCount = 2;
                    break;
                case ComparisonType.Greater_or_equal:
                    cmpSign = ">=";
                    paramCount = 2;
                    break;
                case ComparisonType.Less_or_equal:
                    cmpSign = "<=";
                    paramCount = 2;
                    break;
                case ComparisonType.Less:
                    cmpSign = "<";
                    paramCount = 2;
                    break;
                case ComparisonType.Greater:
                    cmpSign = ">";
                    paramCount = 2;
                    break;
                case ComparisonType.TrueFalse:
                    paramCount = 1;
                    break;
            }

            for (int i = 0; i < paramCount; i++)
            {
                var sp = STACK.Pop();
                switch (sp.Type)
                {
                    case SPType.Variable:
                        var variable = ((spVariable)sp.Value);
                        parameters.Add($"{(variable.Global ? "global" : "local")}Var{variable.ID}");
                        break;
                    case SPType.Integer:
                        parameters.Add(sp.Value.ToString());
                        break;
                    case SPType.Short:
                        parameters.Add(sp.Value.ToString());
                        break;
                    case SPType.Function:
                        parameters.Add($"func_{sp.Value}({STACK.Pop().Value:X})");
                        break;
                }
            }

            parameters.RemoveAll(x => x == "");
            if (parameters.Count > 1)
                parameters.Insert(1, cmpSign);

            return $"if({string.Join(" ", parameters)})\n{{\n";
        }
        private string Decompile_1C(MSCCommand cmd)
        {

            var text = $"{((byte)cmd.Parameters[0] > 0 ? "global" : "local")}Var{cmd.Parameters[2]}";
            var final = "";
            if (STACK.Peek().Value is spVariable)
                final = $"{text} = {STACK.Pop().Value}\n";
            else
                final = $"{text} = 0x{STACK.Pop().Value:X}\n";
            
            return final;
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
        public struct spFunction
        {
            public spFunction(int unk, int id)
            {
                Unk = (byte)unk;
                ID = (byte)id;
            }
            public byte Unk;
            public byte ID;
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
}
