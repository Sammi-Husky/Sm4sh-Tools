using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Sm4shCommand.Classes
{
    public unsafe class CommandList : IDisposable
    {
        public DataSource WorkingSource { get { return _replSource != DataSource.Empty ? _replSource : _workingSource; } }
        public DataSource _workingSource, _replSource;
        public Endianness _endian;

        public CommandList(uint flags, int offset, Endianness endian)
        {
            _flags = flags;
            _offset = offset;
            _endian = endian;
        }
        public CommandList() { }
        ~CommandList() { Dispose(); }

        public int Size
        {
            get
            {
                int size = 0;
                foreach (Command e in Events)
                    size += e.CalcSize();
                return size;
            }
        }

        public bool _empty;


        public uint _flags;
        public int _offset;

        public void OnRebuild(VoidPtr address, int size)
        {
            VoidPtr addr = address;
            for (int x = 0; x < Events.Count; x++)
            {
                byte[] a = Events[x].ToArray();
                byte* tmp = stackalloc byte[a.Length];
                for (int i = 0; i < a.Length; i++)
                    tmp[i] = a[i];

                Win32.MoveMemory(addr, tmp, (uint)a.Length);
                addr += Events[x].CalcSize();
            }
        }
        public void Export(string path)
        {
            byte[] file = new byte[WorkingSource.Length];
            for (int i = 0; i < WorkingSource.Length; i++)
                file[i] = *(byte*)(WorkingSource.Address + i);
            File.WriteAllBytes(path, file);

        }

        public List<Command> Events = new List<Command>();
        public void Dispose()
        {
            _workingSource.Close();
            _replSource.Close();
            GC.SuppressFinalize(this);
        }
    }
}
