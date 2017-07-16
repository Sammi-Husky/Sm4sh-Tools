// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace SALT.Moveset.AnimCMD
{
    /// <summary>
    /// Animation name CRC table.
    /// </summary>
    public unsafe class MTable : IEnumerable
    {
        public MTable(List<uint> cRCTable, Endianness endian)
        {
            this._endian = endian;
            this._baseList = cRCTable;
        }
        public MTable(string path, Endianness endian)
        {
            _endian = endian;
            using (var stream = File.Open(path, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position != stream.Length)
                        this.Add(reader.ReadUInt32(endian));
                }
            }
        }

        public Endianness _endian;
        private List<uint> _baseList = new List<uint>();
        public uint this[int i]
        {
            get { return this._baseList[i]; }
            set { this._baseList[i] = value; }
        }

        public int Count { get { return this._baseList.Count; } }

        public List<uint> ToList()
        {
            return _baseList;
        }
        public uint[] ToArray()
        {
            return _baseList.ToArray();
        }

        public void Export(string path)
        {
            byte[] mtable = new byte[this._baseList.Count * 4];
            int p = 0;
            foreach (uint val in this._baseList)
            {
                byte[] tmp = BitConverter.GetBytes(val);
                if (this._endian == Endianness.Big)
                    Array.Reverse(tmp);

                for (int i = 0; i < 4; i++)
                    mtable[p + i] = tmp[i];
                p += 4;
            }

            File.WriteAllBytes(path, mtable);
        }

        #region IEnumerable implementation
        public void Clear()
        {
            this._baseList = new List<uint>();
        }

        public void Add(uint var)
        {
            this._baseList.Add(var);
        }

        public void Remove(uint var)
        {
            this._baseList.Remove(var);
        }

        public void Remove(int index)
        {
            this._baseList.RemoveAt(index);
        }

        public bool Contains(uint value)
        {
            return _baseList.Contains(value);
        }

        public int IndexOf(uint var)
        {
            return this._baseList.IndexOf(var);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }

        public MTableEnumerator GetEnumerator()
        {
            return new MTableEnumerator(this._baseList.ToArray());
        }

        public class MTableEnumerator : IEnumerator
        {
            public MTableEnumerator(uint[] data)
            {
                this._data = data;
            }

            public uint[] _data;
            private int position = -1;

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }
            public uint Current
            {
                get
                {
                    try
                    {
                        return this._data[this.position];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }


            public bool MoveNext()
            {
                this.position++;
                return (this.position < this._data.Length);
            }
            public void Reset()
            {
                this.position = -1;
            }
        }
        #endregion
    }
}
