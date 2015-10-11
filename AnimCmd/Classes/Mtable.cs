using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace Sm4shCommand.Classes
{
    /// <summary>
    /// Animation name CRC table.
    /// </summary>
    public unsafe class MTable : IEnumerable
    {
        public Endianness _endian;
        private List<uint> _baseList = new List<uint>();
        public uint this[int i]
        {
            get { return _baseList[i]; }
            set { _baseList[i] = value; }
        }
        public MTable(List<uint> CRCTable, Endianness endian)
        {
            _endian = endian;
            _baseList = CRCTable;
        }
        public MTable() { }

        public void Export(string path)
        {
            byte[] mtable = new byte[_baseList.Count * 4];
            int p = 0;
            foreach (uint val in _baseList)
            {
                byte[] tmp = BitConverter.GetBytes(val);
                if (_endian == Endianness.Big)
                    Array.Reverse(tmp);

                for (int i = 0; i < 4; i++)
                    mtable[p + i] = tmp[i];
                p += 4;
            }

            File.WriteAllBytes(path, mtable);
        }
        public void Clear()
        {
            _baseList = new List<uint>();
        }
        public void Add(uint var)
        {
            _baseList.Add(var);
        }
        public void Remove(uint var)
        {
            _baseList.Remove(var);
        }
        public void Remove(int index)
        {
            _baseList.RemoveAt(index);
        }
        public int IndexOf(uint var)
        {
            return _baseList.IndexOf(var);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
        public MTableEnumerator GetEnumerator()
        {
            return new MTableEnumerator(_baseList.ToArray());
        }
        public class MTableEnumerator : IEnumerator
        {
            public uint[] _data;
            int position = -1;
            public MTableEnumerator(uint[] data)
            {
                _data = data;
            }

            public bool MoveNext()
            {
                position++;
                return (position < _data.Length);
            }

            public void Reset()
            {
                position = -1;
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public uint Current
            {
                get
                {
                    try
                    {
                        return _data[position];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
    }
}
