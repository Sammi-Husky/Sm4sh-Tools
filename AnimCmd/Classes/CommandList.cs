using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using System.IO;

namespace Sm4shCommand.Classes
{
    public unsafe class CommandList : IEnumerable<Command>
    {
        private byte[] _data;

        public CommandList(uint CRC)
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
                foreach (Command e in _commands)
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
                byte[] data = ToArray();
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
            _data = ToArray();
        }
        /// <summary>
        /// Rebuilds data, applying changes made
        /// </summary>
        /// <param name="address"></param>
        /// <param name="size"></param>
        public void Rebuild(VoidPtr address, int size)
        {
            VoidPtr addr = address;
            for (int x = 0; x < _commands.Count; x++)
            {
                byte[] a = _commands[x].GetArray();
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
        public void Export(string path)
        {
            byte[] file = ToArray();
            File.WriteAllBytes(path, file);
        }
        /// <summary>
        /// Returns an array of bytes representing this object.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            byte[] file = new byte[Size];

            int i = 0;
            foreach (Command c in _commands)
            {
                byte[] command = c.GetArray();
                for (int x = 0; x < command.Length; x++, i++)
                    file[i] = command[x];
            }
            return file;
        }

        public Command this[int i]
        {
            get { return _commands[i]; }
            set { _commands[i] = value; }
        }
        private List<Command> _commands = new List<Command>();


        #region IEnumerable Implemntation
        public int Count { get { return _commands.Count; } }

        public bool IsReadOnly { get { return false; } }
        public void Clear()
        {
            _commands.Clear();
        }
        public void Insert(int index, Command var)
        {
            _commands.Insert(index, var);
        }
        public void InsertAfter(int index, Command var)
        {
            _commands.Insert(index + 1, var);
        }
        public void Add(Command var)
        {
            _commands.Add(var);
        }
        public bool Remove(Command var)
        {
            return _commands.Remove(var);
        }
        public void RemoveAt(int index) { }
        public void Remove(int index)
        {
            _commands.RemoveAt(index);
        }
        public bool Contains(Command var) { return _commands.Contains(var); }
        public int IndexOf(Command var)
        {
            return _commands.IndexOf(var);
        }
        public void CopyTo(Command[] var, int index) { _commands.CopyTo(var, index); }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<Command> GetEnumerator()
        {
            for (int i = 0; i < _commands.Count; i++)
                yield return _commands[i];
        }
        #endregion
    }
}
