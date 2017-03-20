// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Scripting.MSC
{
    public class MSCScript : IEnumerable<ICommand>, IScript
    {
        public MSCScript()
        {
            Strings = new List<string>();
            Commands = new List<ICommand>();
        }
        public MSCScript(int index, uint offset) : this()
        {
            ScriptIndex = index;
            ScriptOffset = offset;
        }
        public MSCFile File { get; set; }

        public int Size
        {
            get
            {
                int total = 0;
                foreach (var cmd in this.Commands)
                    total += cmd.Size;
                return total;
            }
        }
        public bool IsEntrypoint { get; set; }
        public int ScriptIndex { get; set; }
        public uint ScriptOffset { get; set; }

        public List<ICommand> Commands { get; set; }
        public List<string> Strings { get; set; }

        public byte[] GetBytes(System.IO.Endianness endian)
        {
            List<byte> data = new List<byte>();
            foreach (var cmd in this.Commands)
                data.AddRange(cmd.GetBytes(endian));
            return data.ToArray();
        }

        public string Deserialize()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Commands.Count; i++)
            {
                var cmd = Commands[i];
                sb.Append(cmd.ToString() + $"// {((MSCCommand)cmd).FileOffset - 0x30:X}" + Environment.NewLine);
            }
            return sb.ToString();
        }
        public string Decompile()
        {
            MSCDecompiler d = new MSCDecompiler();
            return d.Decompile(this);
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

                MSCCommand cmd = this.ParseCMD(lines[i]);
                uint ident = cmd.Ident;
                this.Add(cmd);
            }
        }
        private bool IsCMDHandled(uint ID)
        {
            switch (ID)
            {
                case 0x2C:
                    return true;
                default:
                    return false;
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

        public ICommand this[int i]
        {
            get { return this.Commands[i]; }
            set { this.Commands[i] = value; }
        }
        #region IEnumerable Implemntation
        public int Count { get { return this.Commands.Count; } }

        public bool IsReadOnly { get { return false; } }

        public void Clear()
        {
            this.Commands.Clear();
        }

        public void Insert(int index, MSCCommand var)
        {
            this.Commands.Insert(index, var);
        }

        public void InsertAfter(int index, MSCCommand var)
        {
            this.Commands.Insert(index + 1, var);
        }

        public void Add(MSCCommand var)
        {
            this.Commands.Add(var);
        }

        public bool Remove(MSCCommand var)
        {
            return this.Commands.Remove(var);
        }

        public void RemoveAt(int index) { }
        public void Remove(int index)
        {
            this.Commands.RemoveAt(index);
        }

        public bool Contains(MSCCommand var) { return this.Commands.Contains(var); }
        public int IndexOf(MSCCommand var)
        {
            return this.Commands.IndexOf(var);
        }

        public void CopyTo(MSCCommand[] var, int index) { this.Commands.CopyTo(var, index); }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<ICommand> GetEnumerator()
        {
            for (int i = 0; i < this.Commands.Count; i++)
                yield return (MSCCommand)this.Commands[i];
        }
        #endregion
    }
}
