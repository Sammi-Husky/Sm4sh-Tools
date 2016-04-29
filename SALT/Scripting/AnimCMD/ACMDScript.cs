using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace SALT.Scripting.AnimCMD
{
    public unsafe class ACMDScript : IEnumerable<ACMDCommand>
    {
        private byte[] _data;

        public ACMDScript(uint CRC)
        {
            AnimationCRC = CRC;
        }
        /// <summary>
        /// Returns size in bytes.
        /// </summary>
        public int Size
        {
            get
            {
                int size = 0;
                foreach (ACMDCommand e in _commands)
                    size += e.CalcSize();
                return size;
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
        /// <param name="size"></param>
        public void Rebuild(VoidPtr address, int size, Endianness endian)
        {
            VoidPtr addr = address;
            for (int x = 0; x < _commands.Count; x++)
            {
                byte[] a = _commands[x].GetBytes(endian);
                byte* tmp = stackalloc byte[a.Length];
                for (int i = 0; i < a.Length; i++)
                    tmp[i] = a[i];

                Win32.MoveMemory(addr, tmp, (uint)a.Length);
                addr += _commands[x].CalcSize();
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

        public ACMDCommand this[int i]
        {
            get { return _commands[i]; }
            set { _commands[i] = value; }
        }
        private List<ACMDCommand> _commands = new List<ACMDCommand>();


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
        public IEnumerator<ACMDCommand> GetEnumerator()
        {
            for (int i = 0; i < _commands.Count; i++)
                yield return _commands[i];
        }
        #endregion
    }
}
