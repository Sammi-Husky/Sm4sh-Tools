using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using System.IO;

namespace Sm4shCommand.Classes
{
    public unsafe class CommandList : IEnumerable
    {
        private byte[] _data;

        public CommandList(uint CRC)
        {
            AnimationCRC = CRC;
        }
        public CommandList() { }

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

        public bool isEmpty;

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

        public Command this[int i]
        {
            get { return _commands[i]; }
            set { _commands[i] = value; }
        }

        public void Initialize()
        {
            _data = ToArray();
        }
        public void Rebuild(VoidPtr address, int size)
        {
            VoidPtr addr = address;
            for (int x = 0; x < _commands.Count; x++)
            {
                byte[] a = _commands[x].ToArray();
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
                byte[] command = c.ToArray();
                for (int x = 0; x < command.Length; x++, i++)
                    file[i] = command[x];
            }
            return file;
        }

        private List<Command> _commands = new List<Command>();

        #region IEnumerable Implemntation
        public int Count { get { return _commands.Count; } }
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
        public void Remove(Command var)
        {
            _commands.Remove(var);
        }
        public void Remove(int index)
        {
            _commands.RemoveAt(index);
        }
        public int IndexOf(Command var)
        {
            return _commands.IndexOf(var);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
        public CommandListEnumerator GetEnumerator()
        {
            return new CommandListEnumerator(_commands.ToArray());
        }
        public class CommandListEnumerator : IEnumerator
        {
            public Command[] _data;
            int position = -1;
            public CommandListEnumerator(Command[] data)
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

            public Command Current
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
        #endregion
    }


}
