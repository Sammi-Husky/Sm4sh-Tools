// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SALT.Moveset.AnimCMD
{
    public unsafe class ACMDFile : BaseFile, IScriptCollection
    {
        public ACMDFile()
        {
            this.Scripts = new SortedList<uint, IScript>();
            this.AnimationHashPairs = new Dictionary<uint, string>();
        }

        public ACMDFile(DataSource source)
            : this()
        {
            this._workingSource = source;

            if (*(byte*)(source.Address + 0x04) == 0x02)
            {
                this.Endian = Endianness.Little;
            }
            else if (*(byte*)(source.Address + 0x04) == 0x00)
            {
                this.Endian = Endianness.Big;
            }

            this.ActionCount = Util.GetWordUnsafe(source.Address + 0x08, this.Endian);
            this.CommandCount = Util.GetWordUnsafe(source.Address + 0x0C, this.Endian);

            for (int i = 0; i < this.ActionCount; i++)
            {
                uint _crc = (uint)Util.GetWordUnsafe(this._workingSource.Address + 0x10 + (i * 8), this.Endian);
                int _offset = Util.GetWordUnsafe((this._workingSource.Address + 0x10 + (i * 8)) + 0x04, this.Endian);

                this.Scripts.Add(_crc, this.ParseEventList(_crc, _offset));
            }
        }

        public ACMDFile(string filepath)
            : this(new DataSource(FileMap.FromFile(filepath)))
        {
        }

        private DataSource WorkingSource
        {
            get
            {
                return this._replSource != DataSource.Empty ?
                    this._replSource : this._workingSource;
            }
        }
        private DataSource _workingSource, _replSource;

        public Endianness Endian { get; set; }

        public int CommandCount { get; set; }

        public int ActionCount { get; set; }

        /// <summary>
        /// List of all CommandLists in this file.
        /// </summary>
        public SortedList<uint, IScript> Scripts { get; set; }

        /// <summary>
        /// Linked list containing all animation names and their CRC32 hash.
        /// </summary>
        public Dictionary<uint, string> AnimationHashPairs { get; set; }

        /// <summary>
        /// Total Size in bytes.
        /// </summary>
        /// 
        public override int CalcSize() =>
            0x10 + (this.Scripts.Count * 8) + this.Scripts.Values.Sum(e => e.Size);


        /// <summary>
        /// True if the file has changes.
        /// </summary>
        public bool Dirty => this.Scripts.Values.Any(cl => ((ACMDScript)cl).Dirty);

        /// <summary>
        /// Applies changes.
        /// </summary>
        public void Rebuild()
        {
            FileMap temp = FileMap.FromTempFile(this.Size);

            // Write changes to the new filemap.
            this.OnRebuild(temp.Address, temp.Length);

            // Close backing source.
            this._replSource.Close();
            // set backing source to new source from temp map.
            this._replSource = new DataSource(temp.Address, temp.Length) { Map = temp };
            // Set backing source's map to the temp map.
        }
        public override byte[] GetBytes()
        {
            Rebuild();
            return WorkingSource.ToArray();
        }
        /// <summary>
        /// Applies changes and then exports data to file.
        /// </summary>
        /// <param name="path"></param>
        public override void Export(string path)
        {
            this.Rebuild();

            if (this._replSource == DataSource.Empty)
                return;

            this._workingSource.Close();
            this._workingSource = this._replSource;
            this._replSource = DataSource.Empty;


            DataSource src = this._workingSource;
            byte[] tmp = new byte[this.Size];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = *(byte*)(src.Address + i);
            File.WriteAllBytes(path, tmp);
        }
        public void Export(string path, Endianness endian)
        {
            this.Endian = endian;
            this.Export(path);
        }

        /// <summary>
        /// Returns an array of bytes representing this ACMDFile.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes(Endianness endian)
        {
            this.Rebuild();
            DataSource src = this.WorkingSource;
            byte[] tmp = new byte[this.Size];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = *(byte*)(src.Address + i);

            return endian == Endianness.Little ?
                tmp : tmp.Reverse().ToArray();
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();

            foreach (uint u in this.Scripts.Keys)
            {
                string label = string.Empty;
                this.AnimationHashPairs.TryGetValue(u, out label);
                if (string.IsNullOrEmpty(label))
                    label = $"{u:X8}";

                sb.Append(string.Format($"\n\n{this.Scripts.Keys.IndexOf(u):X}: [{label}]"));

                sb.Append("\n\tScript:{");
                if (this.Scripts[u] != null)
                {
                    foreach (ACMDCommand cmd in this.Scripts[u])
                        sb.Append(string.Format("\n\t\t{0}", cmd.ToString()));
                }
                else
                {
                    sb.Append("\n\t\tEmpty");
                }

                sb.Append("\n\t}");
            }

            return sb.ToString();
        }

        private void OnRebuild(VoidPtr address, int length)
        {
            VoidPtr addr = address; // Base address. (0x00)
            Util.SetWordUnsafe(address, 0x444D4341, Endianness.Little); // ACMD

            //==========================================================================//
            //                      Rebuilding Header and offsets                       //
            //==========================================================================//

            Util.SetWordUnsafe(address + 0x04, 2, this.Endian); // Version (2)
            Util.SetWordUnsafe(address + 0x08, this.Scripts.Count, this.Endian);

            int count = this.Scripts.Values.Sum(e => e.Count());

            Util.SetWordUnsafe(address + 0x0C, count, this.Endian);
            addr += 0x10;

            //===============Write Event List offsets and CRC's=================//
            for (int i = 0, prev = 0; i < this.Scripts.Count; i++)
            {
                int dataOffset = 0x10 + (this.Scripts.Count * 8) + prev;
                Util.SetWordUnsafe(addr, (int)this.Scripts.Keys[i], this.Endian);
                Util.SetWordUnsafe(addr + 4, dataOffset, this.Endian);
                prev += this.Scripts.Values[i].Size;
                addr += 8;
            }

            // Write event lists at final address.
            foreach (ACMDScript e in this.Scripts.Values)
            {
                e.Rebuild(addr, e.Size, this.Endian);
                addr += e.Size;
            }
        }
        private ACMDScript ParseEventList(uint cRC, int offset)
        {
            ACMDScript _list = new ACMDScript(cRC);

            ACMDCommand c;

            VoidPtr addr = (this._workingSource.Address + offset);

            // Loop through Event List.
            while (Util.GetWordUnsafe(addr, this.Endian) != 0x5766F889)
            {
                // Try to get command definition
                uint ident = (uint)Util.GetWordUnsafe(addr, this.Endian);
                // Get command parameters and add the command to the event list.
                c = new ACMDCommand(ident);
                for (int i = 0; i < c.ParamSpecifiers.Length; i++)
                {
                    switch (c.ParamSpecifiers[i])
                    {
                        case 0:
                            c.Parameters.Add(Util.GetWordUnsafe(0x04 + (addr + (i * 4)), this.Endian));
                            break;
                        case 1:
                            c.Parameters.Add(Util.GetFloatUnsafe(0x04 + (addr + (i * 4)), this.Endian));
                            break;
                        case 2:
                            c.Parameters.Add((decimal)Util.GetWordUnsafe(0x04 + (addr + (i * 4)), this.Endian));
                            break;
                        case 3:
                            c.Parameters.Add(new FighterVariable((uint)Util.GetWordUnsafe(0x04 + (addr + (i * 4)), this.Endian)));
                            break;
                        default:
                            goto case 0;
                    }
                }

                _list.Add(c);
                addr += c.Size;
            }

            // If we hit a script_end command, add it to the the Event List and terminate looping.
            if (Util.GetWordUnsafe(addr, this.Endian) == 0x5766F889)
            {
                c = new ACMDCommand(0x5766F889);
                _list.Add(c);
            }

            _list.Initialize();
            return _list;
        }
    }
}
