using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Scripting.MSC
{
    public class MSCScript : IEnumerable<ICommand>, IScript
    {
        public int Size
        {
            get
            {
                int total = 0;
                foreach (var cmd in _commands)
                    total += cmd.Size;
                return total;
            }
        }
        public ICommand this[int i]
        {
            get { return _commands[i]; }
            set { _commands[i] = value; }
        }
        public List<ICommand> Commands { get { return _commands; } set { _commands = value; } }
        private List<ICommand> _commands = new List<ICommand>();

        public byte[] GetBytes(System.IO.Endianness endian)
        {
            List<byte> data = new List<byte>();
            foreach (var cmd in _commands)
                data.AddRange(cmd.GetBytes(endian));
            return data.ToArray();
        }
        public string Deserialize()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ICommand cmd in _commands)
                sb.Append(cmd.ToString() + Environment.NewLine);
            return sb.ToString();
        }
        public void Serialize(string text)
        {
            List<string> lines = text.Split('\n').Select(x => x.Trim()).ToList();
            lines.RemoveAll(x => string.IsNullOrEmpty(x));
            this.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                string lineText = lines[i].Trim();
                if (lineText.StartsWith("//"))
                    continue;

                MSCCommand cmd = ParseCMD(lines[i]);
                uint ident = cmd.Ident;
                this.Add(cmd);
            }
        }
        private MSCCommand ParseCMD(string line)
        {
            string s = line.TrimStart();
            s = s.Substring(s.IndexOf('('));
            var name = line.Substring(0, line.IndexOf('('));
            var parameters =
                s.TrimStart('(').TrimEnd(')').Split(',').Select(x =>
                x.Remove(0, x.IndexOf('=') + 1)).ToArray();

            var crc = MSC_INFO.NAMES.Single(x => x.Value == name).Key;
            MSCCommand cmd = new MSCCommand(crc);
            for (int i = 0; i < cmd.ParamSpecifiers.Length; i++)
            {
                switch (cmd.ParamSpecifiers[i])
                {
                    case "B":
                        cmd.Parameters.Add((byte)int.Parse(parameters[i].Substring(2), System.Globalization.NumberStyles.HexNumber));
                        break;
                    case "I":
                        cmd.Parameters.Add(int.Parse(parameters[i].Substring(2), System.Globalization.NumberStyles.HexNumber));
                        break;
                    case "f":
                        cmd.Parameters.Add(float.Parse(parameters[i]));
                        break;
                }
            }
            return cmd;
        }
        #region IEnumerable Implemntation
        public int Count { get { return _commands.Count; } }

        public bool IsReadOnly { get { return false; } }
        public void Clear()
        {
            _commands.Clear();
        }
        public void Insert(int index, MSCCommand var)
        {
            _commands.Insert(index, var);
        }
        public void InsertAfter(int index, MSCCommand var)
        {
            _commands.Insert(index + 1, var);
        }
        public void Add(MSCCommand var)
        {
            _commands.Add(var);
        }
        public bool Remove(MSCCommand var)
        {
            return _commands.Remove(var);
        }
        public void RemoveAt(int index) { }
        public void Remove(int index)
        {
            _commands.RemoveAt(index);
        }
        public bool Contains(MSCCommand var) { return _commands.Contains(var); }
        public int IndexOf(MSCCommand var)
        {
            return _commands.IndexOf(var);
        }
        public void CopyTo(MSCCommand[] var, int index) { _commands.CopyTo(var, index); }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<ICommand> GetEnumerator()
        {
            for (int i = 0; i < _commands.Count; i++)
                yield return (MSCCommand)_commands[i];
        }
        #endregion
    }
}
