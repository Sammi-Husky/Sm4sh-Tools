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
        public DataSource WorkingSource { get { return _workingSource; } }
        public DataSource _workingSource;

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
                int size =0;
                foreach (Command e in Events)
                    size += e.CalcSize();
                return size;
            }
        }
        public uint _flags;
        public int _offset;

        public List<Command> Events = new List<Command>();
    }
}
