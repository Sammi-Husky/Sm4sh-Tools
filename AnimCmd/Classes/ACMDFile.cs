using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sm4shCommand.Classes
{
    public unsafe class ACMDFile
    {
        private VoidPtr Header { get { return _replSource != DataSource.Empty ? _replSource.Address : WorkingSource.Address; } }
        private DataSource WorkingSource, _replSource;

        public int CommandCount { get { return _commandCount; } set { _commandCount = value; } }
        private int _commandCount;

        public int ActionCount { get { return _actionCount; } set { _actionCount = value; } }
        private int _actionCount;

        public Endianness Endian;

        /// <summary>
        /// List of all EventLists in this file.
        /// </summary>
        public SortedList<uint, CommandList> EventLists = new SortedList<uint, CommandList>();

        /// <summary>
        /// Total size in bytes.
        /// </summary>
        public int Size
        {
            get
            {
                int size = 0x10 + (EventLists.Count * 8);
                foreach (CommandList e in EventLists.Values)
                    size += e.Size;
                return size;
            }
        }
        /// <summary>
        /// True if the file has changes.
        /// </summary>
        public bool Dirty
        {
            get
            {
                foreach (CommandList cl in EventLists.Values)
                    if (cl.Dirty)
                        return true;
                return false;
            }
        }

        public ACMDFile(DataSource source, Endianness endian)
        {
            WorkingSource = source;
            Endian = endian;

            _actionCount = Util.GetWordUnsafe(source.Address + 0x08, endian);
            _commandCount = Util.GetWordUnsafe(source.Address + 0x0C, endian);

            Initialize();
        }
        private void Initialize()
        {
            for (int i = 0; i < _actionCount; i++)
            {
                uint _crc = (uint)Util.GetWordUnsafe(WorkingSource.Address + 0x10 + (i * 8), Endian);
                int _offset = Util.GetWordUnsafe((WorkingSource.Address + 0x10 + (i * 8)) + 0x04, Endian);

                EventLists.Add(_crc, ParseEventList(_crc, _offset));
            }
        }

        /// <summary>
        /// Applies changes.
        /// </summary>
        public void Rebuild()
        {
            FileMap temp = FileMap.FromTempFile(Size);

            // Write changes to the new filemap.
            OnRebuild(temp.Address, temp.Length);

            // Close backing source.
            _replSource.Close();
            // set backing source to new source from temp map.
            _replSource = new DataSource(temp.Address, temp.Length);
            // Set backing source's map to the temp map.
            _replSource.Map = temp;
        }

        private void OnRebuild(VoidPtr address, int length)
        {
            //  Make sure empty event lists at least contain the ending specifier,
            //  otherwise the list will bleed over and read the next one.
            for (int i = 0; i < EventLists.Count; i++)
                if (EventLists.Values[i].isEmpty)
                    EventLists.Values[i].Commands.Add(new Command() { _commandInfo = Runtime._endingCommand });

            VoidPtr addr = address; // Base address. (0x00)
            Util.SetWordUnsafe(address, 0x444D4341, Endianness.Little); // ACMD     

            //==========================================================================//   
            //                      Rebuilding Header and offsets                       //
            //==========================================================================//

            Util.SetWordUnsafe(address + 0x04, 2, Endian); // Version (2)
            Util.SetWordUnsafe(address + 0x08, EventLists.Count, Endian);                 
                                                                                        
            int count = 0;                                                              
            foreach (CommandList e in EventLists.Values)                                   
                count += e.Commands.Count;                                              
                                                                                        
            Util.SetWordUnsafe(address + 0x0C, count, Endian);                         
            addr += 0x10;                                                               
                                                                                        
            //===============Write Event List offsets and CRC's=================//              
            for (int i = 0, prev = 0; i < EventLists.Count; i++)                         
            { 
                int dataOffset = 0x10 + (EventLists.Count * 8) + prev;
                Util.SetWordUnsafe(addr, (int)EventLists.Keys[i], Endian);
                Util.SetWordUnsafe(addr + 4, dataOffset, Endian);
                prev += EventLists.Values[i].Size;
                addr += 8;
            }

            // Write event lists at final address.
            foreach (CommandList e in EventLists.Values)
            {
                e.Rebuild(addr, e.Size);
                addr += e.Size;
            }
        }

        private CommandList ParseEventList(uint CRC, int Offset)
        {
            CommandList _cur = new CommandList(CRC);

            Command c = null;
            UnknownCommand unkC = null;

            VoidPtr addr = (WorkingSource.Address + Offset);

            // Loop through Event List.
            while (Util.GetWordUnsafe(addr, Endian) != Runtime._endingCommand.Identifier)
            {
                // Try to get command definition
                uint ident = (uint)Util.GetWordUnsafe(addr, Endian);
                CommandInfo info = null;
                foreach (CommandInfo e in Runtime.commandDictionary)
                    if (e.Identifier == ident) { info = e; break; }

                // If a command definition exists, use that info to deserialize.
                if (info != null)
                {
                    // If previous commands were unknown, add them here.
                    if (unkC != null)
                    {
                        _cur.Commands.Add(unkC);
                        unkC = null;
                    }

                    // Get command parameters and add the command to the event list.
                    c = new Command(Endian, info);
                    for (int i = 0; i < info.ParamSpecifiers.Count; i++)
                    {
                        if (info.ParamSpecifiers[i] == 0)
                            c.parameters.Add(Util.GetWordUnsafe(0x04 + (addr + (i * 4)), Endian));
                        else if (info.ParamSpecifiers[i] == 1)
                            c.parameters.Add(Util.GetFloatUnsafe(0x04 + (addr + (i * 4)), Endian));
                        else if (info.ParamSpecifiers[i] == 2)
                            c.parameters.Add((decimal)Util.GetWordUnsafe(0x04 + (addr + (i * 4)), Endian));
                    }

                    _cur.Commands.Add(c);
                    addr += c.CalcSize();
                }
                // If there is no command definition, this is unknown data.
                // Add the current word to the unk command and continue adding
                // until we hit a known command
                else if (info == null)
                {
                    if (unkC == null)
                        unkC = new UnknownCommand();
                    unkC.data.Add(Util.GetWordUnsafe(addr, Endian));
                    addr += 0x04;
                }
            }

            // If we hit a script_end command, add it to the the Event List and terminate looping.
            if (Util.GetWordUnsafe(addr, Endian) == Runtime._endingCommand.Identifier)
            {
                CommandInfo info = null;

                foreach (CommandInfo e in Runtime.commandDictionary)
                    if (e.Identifier == Runtime._endingCommand.Identifier)
                    { info = e; break; }

                c = new Command(Endian, info);
                _cur.Commands.Add(c);
                addr += 4;
            }
            _cur.Initialize();
            return _cur;
        }

        /// <summary>
        /// Applies changes and then exports data to file.
        /// </summary>
        /// <param name="path"></param>    
        public void Export(string path)
        {
            Rebuild();
            if (_replSource != DataSource.Empty)
            {
                WorkingSource.Close();
                WorkingSource = _replSource;
                _replSource = DataSource.Empty;


                DataSource src = WorkingSource;
                byte[] tmp = new byte[Size];
                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = *(byte*)(src.Address + i);
                File.WriteAllBytes(path, tmp);
            }
        }

        /// <summary>
        /// Returns an array of bytes representing this ACMDFile.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            DataSource src = WorkingSource;
            byte[] tmp = new byte[Size];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = *(byte*)(src.Address + i);
            return tmp;
        }

    }
}
