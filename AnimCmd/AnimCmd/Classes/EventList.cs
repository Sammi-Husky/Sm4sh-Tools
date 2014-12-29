using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimCmd.Structs;

namespace AnimCmd.Classes
{
    public unsafe class EventList
    {
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
                foreach (Event e in Events)
                    size += e.CalcSize();
                return size;
            }
        }
        public uint _flags;
        public int _offset;

        public List<Event> Events = new List<Event>();
    }
}
