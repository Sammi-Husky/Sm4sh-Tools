// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;

namespace SALT.Moveset.AnimCMD
{
    public unsafe class ACMDScript : IEnumerable<ICommand>, IScript
    {
        private byte[] _data;

        public ACMDScript(uint cRC)
        {
            this.AnimationCRC = cRC;
        }

        /// <summary>
        /// Returns Size in bytes.
        /// </summary>
        public int Size
        {
            get
            {
                int Size = 0;
                foreach (ACMDCommand e in this._commands)
                    Size += e.Size;
                return Size;
            }
        }

        /// <summary>
        /// Returns true if the List is empty
        /// </summary>
        public bool Empty { get { return this._commands.Count == 0; } }

        /// <summary>
        /// True if event list has changes.
        /// </summary>
        public bool Dirty
        {
            get
            {
                byte[] data = this.GetBytes(Endianness.Big);
                if (data.Length != this._data.Length)
                    return true;

                for (int i = 0; i < this._data.Length; i++)
                {
                    if (data[i] != this._data[i])
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// CRC32 of the animation name linked to this list of commands.
        /// </summary>
        public uint AnimationCRC;

        public void Initialize()
        {
            this._data = this.GetBytes(Endianness.Big);
        }

        /// <summary>
        /// Rebuilds data, applying changes made
        /// </summary>
        /// <param name="address">Destination address of rebuilt data</param>
        /// <param name="size">Size of the rebuild space</param>
        public void Rebuild(VoidPtr address, int size, Endianness endian)
        {
            VoidPtr addr = address;
            for (int x = 0; x < this._commands.Count; x++)
            {
                byte[] a = this._commands[x].GetBytes(endian);
                byte* tmp = stackalloc byte[a.Length];
                for (int i = 0; i < a.Length; i++)
                    tmp[i] = a[i];

                Win32.MoveMemory(addr, tmp, (uint)a.Length);
                addr += this._commands[x].Size;
            }
        }

        /// <summary>
        /// Applies changes, then exports data to file.
        /// </summary>
        /// <param name="path">Endianess to export the file as</param>
        public void Export(string path, Endianness endian)
        {
            byte[] file = this.GetBytes(endian);
            File.WriteAllBytes(path, file);
        }

        /// <summary>
        /// Returns an array of bytes representing this object.
        /// </summary>
        /// <returns>Byte array representing the file</returns>
        public byte[] GetBytes(Endianness endian)
        {
            byte[] file = new byte[this.Size];

            int i = 0;
            foreach (ACMDCommand c in this._commands)
            {
                byte[] command = c.GetBytes(endian);
                for (int x = 0; x < command.Length; x++, i++)
                    file[i] = command[x];
            }

            return file;
        }

        public ICommand this[int i]
        {
            get { return this._commands[i]; }
            set { this._commands[i] = value; }
        }

        public List<ICommand> Commands { get { return this._commands; } set { this._commands = value; } }
        private List<ICommand> _commands = new List<ICommand>();

        public string Deserialize()
        {
            var tmplines = new List<string>(this.Count);
            for (int i = 0; i < this.Count; i++)
            {
                int amt = 0;

                if ((amt = this.DeserializeCommand(i, this[i].Ident, ref tmplines)) > 0)
                    i += amt;
                else
                    tmplines.Add(this[i].ToString());
            }

            if (this.Empty)
                tmplines.Add("// Empty list");

            this.DoFormat(ref tmplines);
            return string.Join(Environment.NewLine, tmplines);
        }

        public void Serialize(string text)
        {
            this.Serialize(text.Split('\n').Select(x => x.Trim()).ToList());
        }

        public void Serialize(List<string> lines)
        {
            lines.RemoveAll(x => string.IsNullOrEmpty(x));
            this.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                string lineText = lines[i].Trim();
                if (lineText.StartsWith("//"))
                    continue;

                ACMDCommand cmd = this.ParseCMD(lines[i]);
                uint ident = cmd.Ident;


                if (this.SerializeCommands(ref i, ident, ref lines) > 0)
                {
                    continue;
                }
                else
                {
                    this.Add(cmd);
                }
            }
        }

        private int DeserializeCommand(int index, uint ident, ref List<string> lines)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return this.DeserializeConditional(index, ref lines);
                case 0x0EB375E3:
                    return this.DeserializeLoop(index, ref lines);
            }

            return 0;
        }

        private int DeserializeConditional(int startIndex, ref List<string> lines)
        {
            int i = startIndex;

            string str = this[startIndex].ToString();

            int len = (int)this[startIndex].Parameters[0] - 2;
            lines.Add($"{str}{{");
            int count = 0;
            i++;

            while (len > 0)
            {
                len -= this[i].Size / 4;

                if (this[i].Ident == 0x895B9275) // Break if this is an ELSE
                {
                    break;
                }
                else if (IsCmdHandled(this[i].Ident))
                {
                    i += (count += this.DeserializeCommand(i, this[i].Ident, ref lines));
                    count++;
                    break;
                }
                else
                {
                    lines.Add('\t' + this[i].ToString());
                    i++;
                    count++;
                }
            }

            lines.Add("}");
            // if we encountered an else, break out of enclosing bracket scope
            // and then deserialize the else statement
            if (IsCmdConditional(this[i].Ident))
            {
                i += (count += this.DeserializeConditional(i, ref lines));
                count++;
            }
            return count;
        }

        private int DeserializeLoop(int startIndex, ref List<string> lines)
        {
            int i = startIndex;
            int len = 0;

            lines.Add(this[startIndex].ToString() + '{');
            while (this[++i].Ident != 0x38A3EC78)
            {
                len += ((ACMDCommand)this[i]).WordSize;
                if (IsCmdHandled(this[i].Ident))
                    i += this.DeserializeCommand(i, this[i].Ident, ref lines);
                else
                    lines.Add('\t' + this[i].ToString());
            }

            lines.Add('\t' + this[i].ToString());
            lines.Add("}");
            return i - startIndex;
        }
        private int SerializeCommands(ref int index, uint ident, ref List<string> lines)
        {
            if (IsCmdConditional(ident))
                return this.SerializeConditional(ref index, ref lines);
            else if (ident == 0x0EB375E3)
                return this.SerializeLoop(ref index, ref lines);
            else
                return 0;
        }

        private int SerializeConditional(ref int Index, ref List<string> lines)
        {
            ACMDCommand cmd = this.ParseCMD(lines[Index]);
            int len = 2;
            this.Add(cmd);

            while (lines[++Index].Trim() != "}")
            {
                ACMDCommand tmp = this.ParseCMD(lines[Index]);
                if (this.IsCmdHandled(tmp.Ident))
                {
                    len += this.SerializeCommands(ref Index, tmp.Ident, ref lines);
                }
                else
                {
                    len += tmp.Size / 4;
                    this.Add(tmp);
                }
            }

            if (lines[Index + 1] != "}")
            {
                if (ParseCMD(lines[Index + 1]).Ident == 0x895B9275 && cmd.Ident == 0xA5BD4F32)
                {
                    ACMDCommand tmp = this.ParseCMD(lines[++Index]);
                    len += tmp.Size / 4;
                    this[this.IndexOf(cmd)].Parameters[0] = len;
                    len -= tmp.Size / 4;
                    len += this.SerializeCommands(ref Index, tmp.Ident, ref lines);
                }
            }
            else
            {
                this[this.IndexOf(cmd)].Parameters[0] = len;
            }

            // Next line should be closing bracket, ignore and skip it
            return len;
        }

        private int SerializeLoop(ref int Index, ref List<string> lines)
        {
            this.Add(this.ParseCMD(lines[Index]));

            decimal len = 0;
            while (this.ParseCMD(lines[++Index]).Ident != 0x38A3EC78)
            {
                ACMDCommand tmp = this.ParseCMD(lines[Index]);
                if (IsCmdHandled(tmp.Ident))
                {
                    len += this.SerializeCommands(ref Index, tmp.Ident, ref lines);
                }
                else
                {
                    len += (tmp.Size / 4);
                    this.Add(tmp);
                }
            }

            ACMDCommand endLoop = this.ParseCMD(lines[Index]);
            endLoop.Parameters[0] = len / -1;
            this.Add(endLoop);

            if(lines[Index + 1].Trim() == "}")
                Index++;

            // Next line should be closing bracket, ignore and skip it
            return (int)len+4;
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
            if (IsCmdConditional(ident))
                return true;
            else if (ident == 0x0EB375E3) // loop
                return true;
            else
                return false;
        }
        private bool IsCmdConditional(uint ident)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return true;
            }
            return false;
        }
        private void DoFormat(ref List<string> tmplines)
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

        #region IEnumerable Implemntation
        public int Count { get { return this._commands.Count; } }

        public bool IsReadOnly { get { return false; } }
        public void Clear()
        {
            this._commands.Clear();
        }

        public void Insert(int index, ACMDCommand var)
        {
            this._commands.Insert(index, var);
        }

        public void InsertAfter(int index, ACMDCommand var)
        {
            this._commands.Insert(index + 1, var);
        }

        public void Add(ACMDCommand var)
        {
            this._commands.Add(var);
        }

        public bool Remove(ACMDCommand var)
        {
            return this._commands.Remove(var);
        }

        public void RemoveAt(int index) { }
        public void Remove(int index)
        {
            this._commands.RemoveAt(index);
        }

        public bool Contains(ACMDCommand var) { return this._commands.Contains(var); }
        public int IndexOf(ACMDCommand var)
        {
            return this._commands.IndexOf(var);
        }

        public void CopyTo(ACMDCommand[] var, int index) { this._commands.CopyTo(var, index); }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<ICommand> GetEnumerator()
        {
            for (int i = 0; i < this._commands.Count; i++)
                yield return (ACMDCommand)this._commands[i];
        }
        #endregion
    }
}
