using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Moveset.MSC
{
    public class MSCDecompiler
    {
        public MSCDecompiler()
        {
            INDENT_STACK = new List<int>();
            COMMANDS = new Stack<MSCCommand>();
        }

        public MSCScript Target { get; set; }

        public MSCCommandManager Manager { get; set; }

        public List<int> INDENT_STACK { get; set; }
        public Stack<MSCCommand> COMMANDS = null;
        public Stack<MSCCommand> TO_PROCESS = null;

        public string Decompile(MSCScript script)
        {
            var analyzer = new MSCAnalyzer(script);
            analyzer.Analyze_1();
            Target = script;
            Manager = new MSCCommandManager(Target);
            StringBuilder sb = new StringBuilder();

            while (!Manager.End)
            {
                MSCCommand cmd = Manager.Next();

                // Do ending brackets
                while (INDENT_STACK.Contains(cmd.FileOffset - 0x30))
                {
                    INDENT_STACK.Remove(cmd.FileOffset - 0x30);
                    sb.Append(DoIndent("}\n"));
                }

                // If the command is passed to the next add it to the assignment stack
                if (cmd.Returns)
                {
                    COMMANDS.Push(cmd);
                    continue;
                }

                sb.Append(DecompileCMD(cmd) + "\n");

            }
            return sb.ToString();
        }
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
                case 0x14:
                    sb.Append(Decompile_14(cmd));
                    break;
                case 0x15:
                    sb.Append(Decompile_15(cmd));
                    break;
                case 0x16:
                    sb.Append(Decompile_16(cmd));
                    break;
                case 0x1C:
                    sb.Append(Decompile_1C(cmd));
                    break;
                case 0x1D:
                    sb.Append(Decompile_1D(cmd));
                    break;
                case 0x1E:
                    sb.Append(Decompile_1E(cmd));
                    break;
                case 0x1F:
                    sb.Append(Decompile_1F(cmd));
                    break;
                case 0x20:
                    sb.Append(Decompile_20(cmd));
                    break;
                case 0x21:
                    sb.Append(Decompile_21(cmd));
                    break;
                case 0x22:
                    sb.Append(Decompile_22(cmd));
                    break;
                case 0x23:
                    sb.Append(Decompile_23(cmd));
                    break;
                case 0x24:
                    sb.Append(Decompile_24(cmd));
                    break;
                case 0x25:
                    sb.Append(Decompile_25(cmd));
                    break;
                case 0x26:
                    sb.Append(Decompile_26(cmd));
                    break;
                case 0x27:
                    sb.Append(Decompile_27(cmd));
                    break;
                case 0x28:
                    sb.Append(Decompile_28(cmd));
                    break;
                case 0x29:
                    sb.Append(Decompile_29(cmd));
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
                case 0x30:
                    sb.Append(Decompile_30(cmd));
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
                case 0x42:
                    sb.Append(Decompile_42(cmd));
                    break;
                case 0x43:
                    sb.Append(Decompile_43(cmd));
                    break;
                case 0x44:
                    sb.Append(Decompile_44(cmd));
                    break;
                case 0x45:
                    sb.Append(Decompile_45(cmd));
                    break;
                case 0x46:
                    sb.Append(Decompile_46(cmd));
                    break;
                case 0x47:
                    sb.Append(Decompile_47(cmd));
                    break;
                case 0x48:
                    sb.Append(Decompile_48(cmd));
                    break;
                case 0x49:
                    sb.Append(Decompile_49(cmd));
                    break;
                case 0x4a:
                    sb.Append(Decompile_4a(cmd));
                    break;
                case 0x4b:
                    sb.Append(Decompile_4b(cmd));
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
            return $"!{DecompileCMD(COMMANDS.Pop())}";
        } // negate
        private string Decompile_14(MSCCommand cmd)
        {
            return $"{DecompileCMD(COMMANDS.Pop())}++";
        } // ++
        private string Decompile_15(MSCCommand cmd)
        {
                return $"{DecompileCMD(COMMANDS.Pop())}--";
        } // --
        private string Decompile_16(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();
            return $"{DecompileCMD(arg2)} & {DecompileCMD(arg1)}";
        } // &
        private string Decompile_1C(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);

            var arg = COMMANDS.Pop();
            if (arg.Ident == 0x1C)
                return $"{text} = 0x{arg.Parameters[0]:X}";
            else
                return $"{text} = {DecompileCMD(arg)}";
        } // assign_var
        private string Decompile_1D(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} += {DecompileCMD(COMMANDS.Pop())}";
        } // +=
        private string Decompile_1E(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} -= {DecompileCMD(COMMANDS.Pop())}";
        } // -=
        private string Decompile_1F(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} *= {DecompileCMD(COMMANDS.Pop())}";
        } // *=
        private string Decompile_20(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} /= {DecompileCMD(COMMANDS.Pop())}";
        } // /=
        private string Decompile_21(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} %= {DecompileCMD(COMMANDS.Pop())}";
        } // %=
        private string Decompile_22(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} &= {DecompileCMD(COMMANDS.Pop())}";
        } // &=
        private string Decompile_23(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} |= {DecompileCMD(COMMANDS.Pop())}";
        } // |=
        private string Decompile_24(MSCCommand cmd)
        {
            var variable_str = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            return $"{variable_str} ^= {DecompileCMD(COMMANDS.Pop())}";
        } // ^=
        //============== Comparisons ===============//
        private string Decompile_25(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} < {DecompileCMD(arg1)}";
        } // less
        private string Decompile_26(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} <= {DecompileCMD(arg1)}";
        } // less_or_equals
        private string Decompile_27(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} == {DecompileCMD(arg1)}";
        } // equals
        private string Decompile_28(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} != {DecompileCMD(arg1)}";
        } // not_equals
        private string Decompile_29(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} > {DecompileCMD(arg1)}";
        } // greater
        private string Decompile_2A(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();

            return $"{DecompileCMD(arg2)} >= {DecompileCMD(arg1)}";
        } // greater_or_equal
        private string Decompile_2B(MSCCommand cmd)
        {
            var arg = COMMANDS.Pop();
            return $"{DecompileCMD(arg)} == 0";
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
                        parameters.Add(Decompile_0A(arg)/*$"\"{Target.File.Strings[(int)arg.Parameters[0]]}\""*/);
                        break;
                    case 0x0D:
                        parameters.Add($"\"{Target.File.Strings[(short)arg.Parameters[0]]}\"");
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
            string[] parameters = new string[(byte)cmd.Parameters[0]];
            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                parameters[i] = DecompileCMD(COMMANDS.Pop());
            }
            transform_syscall_params((byte)cmd.Parameters[1], ref parameters);
            return $"global_{cmd.Parameters[1]:X}({string.Join(", ", parameters)})";
        } // syscall
        private string Decompile_2E(MSCCommand cmd)
        {

            var str = $"unk_2E\n{DoIndent("{")}";
            INDENT_STACK.Add((int)cmd.Parameters[0]);
            return str;
        } // Try? No parameter code block
        private string Decompile_2F(MSCCommand cmd)
        {
            return Decompile_31(cmd);
            //var parameters = new List<string>();
            //var arg1 = COMMANDS.Pop();
            //for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            //{
            //    parameters.Add(DecompileCMD(COMMANDS.Pop()));
            //}
            //return $"func_{Target.File.Scripts.IndexOfKey((uint)(int)arg1.Parameters[0])}({string.Join(", ", parameters)})";
        } // same as 2D but always comes after 2F?
        private string Decompile_30(MSCCommand cmd)
        {
            return Decompile_31(cmd);
        }
        private string Decompile_31(MSCCommand cmd)
        {
            var arg = COMMANDS.Pop();
            string str = "";
            if (arg.Ident == 0x0B) // if it's a variable containing a funciton pointer
            {
                str = $"func_{DecompileCMD(arg):X}";
            }
            else
            {
                str = $"func_{Target.File.Offsets.IndexOf((uint)(int)arg.Parameters[0])}";
            }
            var parameters = new List<string>();
            for (int i = 0; i < (byte)cmd.Parameters[0]; i++)
            {
                parameters.Add(DecompileCMD(COMMANDS.Pop()));
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
            var str = "";

            INDENT_STACK.RemoveAt(INDENT_STACK.Count - 1);
            //str += DoIndent("}\n");
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
        private string Decompile_42(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            var arg = COMMANDS.Pop();


            return $"{text} = {DecompileCMD(arg)}";
        }
        private string Decompile_43(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            var arg = COMMANDS.Pop();


            return $"{text} = {DecompileCMD(arg)}";
        }
        private string Decompile_44(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            var arg = COMMANDS.Pop();


            return $"{text} = {DecompileCMD(arg)}";
        }
        private string Decompile_45(MSCCommand cmd)
        {
            var text = FormatVariable((byte)cmd.Parameters[0], (byte)cmd.Parameters[1], (byte)cmd.Parameters[2]);
            var arg = COMMANDS.Pop();


            return $"{text} = {DecompileCMD(arg)}";
        }
        private string Decompile_46(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();
            var val = BitConverter.GetBytes(int.Parse(DecompileCMD(arg1).Substring(2), System.Globalization.NumberStyles.HexNumber));
            var f = BitConverter.ToSingle(val, 0);
            return $"{DecompileCMD(arg2)} > {f}";
        } // greater
        private string Decompile_47(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();
            var val = BitConverter.GetBytes(int.Parse(DecompileCMD(arg1).Substring(2), System.Globalization.NumberStyles.HexNumber));
            var f = BitConverter.ToSingle(val, 0);
            return $"{DecompileCMD(arg2)} <= {f}";
        } // less or equal
        private string Decompile_48(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();
            var val = BitConverter.GetBytes(int.Parse(DecompileCMD(arg1).Substring(2), System.Globalization.NumberStyles.HexNumber));
            var f = BitConverter.ToSingle(val, 0);
            return $"{DecompileCMD(arg2)} < {f}";
        } // less
        private string Decompile_49(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();
            var val = BitConverter.GetBytes(int.Parse(DecompileCMD(arg1).Substring(2), System.Globalization.NumberStyles.HexNumber));
            var f = BitConverter.ToSingle(val, 0);
            return $"{DecompileCMD(arg2)} != {f}";
        } // not equal
        private string Decompile_4a(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();
            var val = BitConverter.GetBytes(int.Parse(DecompileCMD(arg1).Substring(2), System.Globalization.NumberStyles.HexNumber));
            var f = BitConverter.ToSingle(val, 0);
            return $"{DecompileCMD(arg2)} == {f}";
        } // equal
        private string Decompile_4b(MSCCommand cmd)
        {
            var arg1 = COMMANDS.Pop();
            var arg2 = COMMANDS.Pop();
            var val = BitConverter.GetBytes(int.Parse(DecompileCMD(arg1).Substring(2), System.Globalization.NumberStyles.HexNumber));
            var f = BitConverter.ToSingle(val, 0);
            return $"{DecompileCMD(arg2)} >= {f}";
        } // not equal


        private void transform_syscall_params(int syscall, ref string[] parameters)
        {
            switch (syscall)
            {
                case 3:
                    transform_sys_3(ref parameters);
                    break;
            }
        }
        private void transform_sys_3(ref string[] parameters)
        {
            uint offset = uint.Parse(parameters[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
            if (Target.File.Offsets.Contains(offset))
            {
                parameters[0] = $"script_{Target.File.Offsets.IndexOf(offset)}";
            }
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

    public class MSCAnalyzer
    {
        public MSCAnalyzer(MSCScript script)
        {
            Target = script;
            _commands = new List<Tuple<int, MSCCommand>>();
            Manager = new MSCCommandManager(Target);
        }

        public MSCScript Target { get; set; }
        MSCCommandManager Manager { get; set; }
        public readonly List<Tuple<int, MSCCommand>> _commands;

        // Phase 1, record each command's bracket scope.
        public void Analyze_1()
        {
            // add everything with an initial scope of 0;
            int i = 0;
            int scopeEnd = -1;
            while (!Manager.End)
            {
                var cmd = Manager.Next();

                // Check if were at the end of the if block and
                // manually break out of enclosing scope if this
                // is an else command

                if (cmd.FileOffset - 0x30 == scopeEnd || cmd.Ident == 0x36)
                {
                    if (cmd.FileOffset - 0x30 == scopeEnd)
                        scopeEnd = -1;

                    i--;
                }

                _commands.Add(new Tuple<int, MSCCommand>(i, cmd));

                if (RaisesScope(cmd))
                {
                    scopeEnd = (int)cmd.Parameters[0] - 4 - cmd.TotalSize;
                    i++;
                }
            }
            Manager.Rewind();
        }

        // Phase 2, reverse and decompile to raw output
        public void Analyze_2()
        {
            // traverse list in reverse order, consuming commands above them as needed.
            var cmd_reversed = new List<Tuple<int, MSCCommand>>(_commands);
            cmd_reversed.Reverse();


        }
        public bool RaisesScope(MSCCommand cmd)
        {
            switch (cmd.Ident)
            {
                case 0x2E:
                case 0x34:
                case 0x36:
                    return true;
                default:
                    return false;
            }
        }
    }

    public class AnalyzerStack : IEnumerable<ICommand>
    {
        public AnalyzerStack(ICollection<ICommand> col)
        {
            _commands = new List<ICommand>(col);
        }
        private readonly List<ICommand> _commands;

        public void Push(ICommand cmd)
        {
            _commands.Add(cmd);
        }
        public void Push(ICommand cmd, int index)
        {
            _commands.Insert(index, cmd);
        }

        public ICommand Pop()
        {
            return Pop(_commands.Count - 1);
        }
        public ICommand Pop(int index)
        {
            var cmd = _commands[index];
            _commands.RemoveAt(index);
            return cmd;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<ICommand> GetEnumerator()
        {
            for (int i = 0; i < this._commands.Count; i++)
                yield return this._commands[i];
        }

        public int Count { get { return _commands.Count; } }
        public ICommand this[int index]
        {
            get
            {
                return _commands[index];
            }
            set
            {
                _commands[index] = value;
            }
        }
    }
    public class MSCConstant
    {
        public MSCConstant(int value) { _value = value; }
        private readonly int _value;

        public static implicit operator MSCConstant(int value)
        {
            return new MSCConstant(value);
        }

        public static MSCConstant operator +(MSCConstant first, MSCConstant second)
        {
            return new MSCConstant(first._value + second._value);
        }
        public static MSCConstant operator -(MSCConstant first, MSCConstant second)
        {
            return new MSCConstant(first._value - second._value);
        }
        public static MSCConstant operator /(MSCConstant first, MSCConstant second)
        {
            return new MSCConstant(first._value / second._value);
        }
        public static MSCConstant operator *(MSCConstant first, MSCConstant second)
        {
            return new MSCConstant(first._value + second._value);
        }

        public int GetValue()
        {
            return _value;
        }
        public override string ToString()
        {
            return _value.ToString();
        }
    }
    public class MSCVariable
    {
        public MSCVariable(MSCVariable value, MSCVariableScope variableType, byte unk, byte id)
        {
            _value = value;
            Unk = unk;
            ID = ID;
            VarType = variableType;
        }
        public MSCVariable(MSCConstant value, MSCVariableScope variableType, byte unk, byte id)
        {
            _value = value;
            Unk = unk;
            ID = ID;
            VarType = variableType;
        }
        public MSCVariable(MSCFunction value, MSCVariableScope variableType, byte unk, byte id)
        {
            _value = value;
            Unk = unk;
            ID = ID;
            VarType = variableType;
        }

        private readonly object _value;

        public MSCVariableScope VarType { get; set; }
        public byte Unk { get; set; }
        public byte ID { get; set; }

        public override string ToString()
        {
            return $"{(VarType > 0 ? "Global" : "Local")}Var{ID}";
        }
    }

    public class MSCFunction
    {
        public MSCFunction(MSCScript target, params object[] parameters)
        {
            TargetScript = target;
            Parameters = parameters;
        }
        public MSCScript TargetScript { get; set; }
        public object[] Parameters { get; set; }

        public override string ToString()
        {
            return $"execute script_{TargetScript.ScriptIndex}({string.Join(",", Parameters)})";
        }
    }

    public enum MSCVariableScope : byte
    {
        Global,
        Local
    }
}
