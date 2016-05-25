using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace SALT.Scripting.AnimCMD
{
    public unsafe class ACMDScript : IEnumerable<ICommand>, IScript
    {
        private byte[] _data;

        public ACMDScript(uint CRC)
        {
            AnimationCRC = CRC;
        }
        /// <summary>
        /// Returns Size in bytes.
        /// </summary>
        public int Size
        {
            get
            {
                int Size = 0;
                foreach (ACMDCommand e in _commands)
                    Size += e.Size;
                return Size;
            }
        }
        /// <summary>
        /// Returns true if the List is empty
        /// </summary>
        public bool Empty { get { return _commands.Count == 0; } }
        /// <summary>
        /// True if event list has changes.
        /// </summary>
        public bool Dirty
        {
            get
            {
                byte[] data = GetBytes(Endianness.Big);
                if (data.Length != _data.Length)
                    return true;

                for (int i = 0; i < _data.Length; i++)
                    if (data[i] != _data[i])
                        return true;

                return false;
            }
        }
        /// <summary>
        /// CRC32 of the animation name linked to this list of commands.
        /// </summary>
        public uint AnimationCRC;

        public void Initialize()
        {
            _data = GetBytes(Endianness.Big);
        }
        /// <summary>
        /// Rebuilds data, applying changes made
        /// </summary>
        /// <param name="address"></param>
        /// <param name="Size"></param>
        public void Rebuild(VoidPtr address, int Size, Endianness endian)
        {
            VoidPtr addr = address;
            for (int x = 0; x < _commands.Count; x++)
            {
                byte[] a = _commands[x].GetBytes(endian);
                byte* tmp = stackalloc byte[a.Length];
                for (int i = 0; i < a.Length; i++)
                    tmp[i] = a[i];

                Win32.MoveMemory(addr, tmp, (uint)a.Length);
                addr += _commands[x].Size;
            }
        }
        /// <summary>
        /// Applies changes, then exports data to file.
        /// </summary>
        /// <param name="path"></param>
        public void Export(string path, Endianness endian)
        {
            byte[] file = GetBytes(endian);
            File.WriteAllBytes(path, file);
        }
        /// <summary>
        /// Returns an array of bytes representing this object.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes(Endianness endian)
        {
            byte[] file = new byte[Size];

            int i = 0;
            foreach (ACMDCommand c in _commands)
            {
                byte[] command = c.GetBytes(endian);
                for (int x = 0; x < command.Length; x++, i++)
                    file[i] = command[x];
            }
            return file;
        }

        public ICommand this[int i]
        {
            get { return _commands[i]; }
            set { _commands[i] = value; }
        }
        public List<ICommand> Commands { get { return _commands; } set { _commands = value; } }
        private List<ICommand> _commands = new List<ICommand>();


        public string Deserialize()
        {
            var tmplines = new List<string>(Count);
            for (int i = 0; i < Count; i++)
            {
                int amt = 0;
                if ((amt = DeserializeCommand(i, this[i].Ident, ref tmplines)) > 0)
                    i += amt;

                if (i < Count)
                    tmplines.Add(this[i].ToString());
            }
            if (Empty)
                tmplines.Add("// Empty list");

            DoFormat(ref tmplines);
            return string.Join(Environment.NewLine, tmplines);
        }
        public void Serialize(string text)
        {
            Serialize(text.Split('\n').Select(x => x.Trim()).ToList());
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

                ACMDCommand cmd = ParseCMD(lines[i]);
                uint ident = cmd.Ident;


                int amt = 0;
                if ((amt = SerializeCommands(i, ident, ref lines)) > 0)
                {
                    i += amt;
                    continue;
                }
                else
                    this.Add(cmd);
            }
        }

        private int DeserializeCommand(int index, uint ident, ref List<string> Lines)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return DeserializeConditional(index, ref Lines);
                case 0x0EB375E3:
                    return DeserializeLoop(index, ref Lines);
            }
            return 0;
        }
        private int DeserializeConditional(int startIndex, ref List<string> Lines)
        {
            int i = startIndex;

            string str = this[startIndex].ToString();
            int len = (int)this[startIndex].Parameters[0] - 2;
            Lines.Add($"{str}{{");
            int count = 1;
            i++;

            while (len > 0)
            {
                len -= this[i].Size / 4;

                if (IsCmdHandled(this[i].Ident))
                    break;
                else
                {
                    Lines.Add('\t' + this[i].ToString());
                    i++;
                    count++;
                }
            }
            if (IsCmdHandled(this[i].Ident))
                i += (count += DeserializeCommand(i, this[i].Ident, ref Lines));
            Lines.Add("}");
            return count;
        }
        private int DeserializeLoop(int startIndex, ref List<string> Lines)
        {
            int i = startIndex;

            string str = this[startIndex].ToString();
            int len = 0;
            str += '{';
            Lines.Add(str);
            while (this[++i].Ident != 0x38A3EC78)
            {
                len += this[i].Size / 4;
                i += DeserializeCommand(i, this[i].Ident, ref Lines);
                Lines.Add('\t' + this[i].ToString());
            }
            Lines.Add('\t' + this[i].ToString());
            Lines.Add("}");
            return ++i - startIndex;
        }

        private int SerializeCommands(int index, uint ident, ref List<string> Lines)
        {
            switch (ident)
            {
                case 0xA5BD4F32:
                case 0x895B9275:
                case 0x870CF021:
                    return SerializeConditional(index, ref Lines);
                case 0x0EB375E3:
                    return SerializeLoop(index, ref Lines);
            }
            return 0;
        }
        private int SerializeConditional(int startIndex, ref List<string> Lines)
        {
            ACMDCommand cmd = ParseCMD(Lines[startIndex]);
            int i = startIndex;
            int len = 2;
            this.Add(cmd);
            while (Lines[++i].Trim() != "}")
            {
                ACMDCommand tmp = ParseCMD(Lines[i]);
                len += tmp.Size / 4;
                if (IsCmdHandled(tmp.Ident))
                    i += SerializeCommands(i, tmp.Ident, ref Lines);
                else
                    this.Add(tmp);
            }
            this[this.IndexOf(cmd)].Parameters[0] = len;
            // Next line should be closing bracket, ignore and skip it
            return i - startIndex;
        }
        private int SerializeLoop(int index, ref List<string> Lines)
        {
            int i = index;
            this.Add(ParseCMD(Lines[i]));
            decimal len = 0;
            while (ParseCMD(Lines[++i]).Ident != 0x38A3EC78)
            {
                ACMDCommand tmp = ParseCMD(Lines[i]);
                len += (tmp.Size / 4);
                i += SerializeCommands(i, tmp.Ident, ref Lines);
                this.Add(tmp);
            }
            ACMDCommand endLoop = ParseCMD(Lines[i]);
            endLoop.Parameters[0] = len / -1;
            this.Add(endLoop);
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
        public int Count { get { return _commands.Count; } }

        public bool IsReadOnly { get { return false; } }
        public void Clear()
        {
            _commands.Clear();
        }
        public void Insert(int index, ACMDCommand var)
        {
            _commands.Insert(index, var);
        }
        public void InsertAfter(int index, ACMDCommand var)
        {
            _commands.Insert(index + 1, var);
        }
        public void Add(ACMDCommand var)
        {
            _commands.Add(var);
        }
        public bool Remove(ACMDCommand var)
        {
            return _commands.Remove(var);
        }
        public void RemoveAt(int index) { }
        public void Remove(int index)
        {
            _commands.RemoveAt(index);
        }
        public bool Contains(ACMDCommand var) { return _commands.Contains(var); }
        public int IndexOf(ACMDCommand var)
        {
            return _commands.IndexOf(var);
        }
        public void CopyTo(ACMDCommand[] var, int index) { _commands.CopyTo(var, index); }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<ICommand> GetEnumerator()
        {
            for (int i = 0; i < _commands.Count; i++)
                yield return (ACMDCommand)_commands[i];
        }
        #endregion
    }
}
