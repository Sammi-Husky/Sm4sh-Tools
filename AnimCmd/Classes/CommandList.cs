using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sm4shCommand.Classes
{
    public unsafe class CommandList
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
                foreach (Command e in Commands)
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


        public uint AnimationCRC;

        public void Initialize()
        {
            _data = ToArray();
        }
        public void Rebuild(VoidPtr address, int size)
        {
            VoidPtr addr = address;
            for (int x = 0; x < Commands.Count; x++)
            {
                byte[] a = Commands[x].ToArray();
                byte* tmp = stackalloc byte[a.Length];
                for (int i = 0; i < a.Length; i++)
                    tmp[i] = a[i];

                Win32.MoveMemory(addr, tmp, (uint)a.Length);
                addr += Commands[x].CalcSize();
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
            foreach (Command c in Commands)
            {
                byte[] command = c.ToArray();
                for (int x = 0; x < command.Length; x++, i++)
                    file[i] = command[x];
            }
            return file;
        }

        public List<Command> Commands = new List<Command>();
    }
}
