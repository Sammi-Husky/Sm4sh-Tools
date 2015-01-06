using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimCmd.Structs;
using System.IO;

namespace AnimCmd.Classes
{
    public unsafe class EventList
    {
        public DataSource WorkingSource { get { return _replSource != DataSource.Empty ? _replSource : _workingSource; } }
        public DataSource _workingSource, _replSource;

        public EventList(TableEntry t)
        {
            _flags = t._flags;
            _offset = t._offset;
        }
        public EventList() { }

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
        public bool _dirty;

        public uint _flags;
        public int _offset;

        public void Rebuild()
        {
            FileMap temp = FileMap.FromTempFile(Size);
            OnRebuild(temp.Address, temp.Length);
            _replSource.Close();
            _replSource = new DataSource(temp.Address, temp.Length);
            _replSource.Map = temp;

        }
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
                _replSource = new DataSource(address, size);
            
        }
        public void Export(string path)
        {
            Rebuild();
            byte[] file = new byte[WorkingSource.Length];
            for (int i = 0; i < WorkingSource.Length; i++)
                file[i] = *(byte*)(WorkingSource.Address + i);
            File.WriteAllBytes(path, file);

        }

        public List<Command> Events = new List<Command>();
    }
}
