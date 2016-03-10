using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sm4shCommand.Classes
{
    public unsafe class ACMDFile
    {
        public ACMDFile()
        {
            _eventLists = new SortedList<uint, ACMDScript>();
            _hashPairs = new Dictionary<uint, string>();
            Type = ACMDType.NONE;
        }
        public ACMDFile(DataSource source) : this()
        {
            _workingSource = source;

            _actionCount = Util.GetWordUnsafe(source.Address + 0x08, Runtime.WorkingEndian);
            _commandCount = Util.GetWordUnsafe(source.Address + 0x0C, Runtime.WorkingEndian);

            for (int i = 0; i < _actionCount; i++)
            {
                uint _crc = (uint)Util.GetWordUnsafe(_workingSource.Address + 0x10 + (i * 8), Runtime.WorkingEndian);
                int _offset = Util.GetWordUnsafe((_workingSource.Address + 0x10 + (i * 8)) + 0x04, Runtime.WorkingEndian);

                EventLists.Add(_crc, ParseEventList(_crc, _offset));
            }
        }
        public ACMDFile(string filepath) : this(new DataSource(FileMap.FromFile(filepath))) { }

        private VoidPtr WorkingSource => _replSource != DataSource.Empty ? _replSource.Address : _workingSource.Address;
        private DataSource _workingSource, _replSource;

        public int CommandCount { get { return _commandCount; } set { _commandCount = value; } }
        private int _commandCount;

        public int ActionCount { get { return _actionCount; } set { _actionCount = value; } }
        private int _actionCount;

        public ACMDType Type { get; set; }

        /// <summary>
        /// List of all CommandLists in this file.
        /// </summary>
        public SortedList<uint, ACMDScript> EventLists { get { return _eventLists; } set { _eventLists = value; } }
        private SortedList<uint, ACMDScript> _eventLists;
        /// <summary>
        /// Linked list containing all animation names and their CRC32 hash.
        /// </summary>
        public Dictionary<uint, string> AnimationHashPairs { get { return _hashPairs; } set { _hashPairs = value; } }
        private Dictionary<uint, string> _hashPairs;
        /// <summary>
        /// Total size in bytes.
        /// </summary>
        public int Size => 0x10 + (EventLists.Count * 8) + EventLists.Values.Sum(e => e.Size);
        /// <summary>
        /// True if the file has changes.
        /// </summary>
        public bool Dirty => EventLists.Values.Any(cl => cl.Dirty);
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
            _replSource = new DataSource(temp.Address, temp.Length) { Map = temp };
            // Set backing source's map to the temp map.
        }
        private void OnRebuild(VoidPtr address, int length)
        {
            //  Remove empty event lists
            for (int i = 0; i < EventLists.Count; i++)
                if (EventLists.Values[i].Empty)
                    EventLists.RemoveAt(i);

            VoidPtr addr = address; // Base address. (0x00)
            Util.SetWordUnsafe(address, 0x444D4341, Endianness.Little); // ACMD     

            //==========================================================================//   
            //                      Rebuilding Header and offsets                       //
            //==========================================================================//

            Util.SetWordUnsafe(address + 0x04, 2, Runtime.WorkingEndian); // Version (2)
            Util.SetWordUnsafe(address + 0x08, EventLists.Count, Runtime.WorkingEndian);

            int count = EventLists.Values.Sum(e => e.Count);

            Util.SetWordUnsafe(address + 0x0C, count, Runtime.WorkingEndian);
            addr += 0x10;

            //===============Write Event List offsets and CRC's=================//              
            for (int i = 0, prev = 0; i < EventLists.Count; i++)
            {
                int dataOffset = 0x10 + (EventLists.Count * 8) + prev;
                Util.SetWordUnsafe(addr, (int)EventLists.Keys[i], Runtime.WorkingEndian);
                Util.SetWordUnsafe(addr + 4, dataOffset, Runtime.WorkingEndian);
                prev += EventLists.Values[i].Size;
                addr += 8;
            }

            // Write event lists at final address.
            foreach (ACMDScript e in EventLists.Values)
            {
                e.Rebuild(addr, e.Size);
                addr += e.Size;
            }
        }
        private ACMDScript ParseEventList(uint CRC, int Offset)
        {
            ACMDScript _list = new ACMDScript(CRC);

            ACMDCommand c;

            VoidPtr addr = (_workingSource.Address + Offset);

            // Loop through Event List.
            while (Util.GetWordUnsafe(addr, Runtime.WorkingEndian) != Runtime._endingCommand.Identifier)
            {
                // Try to get command definition
                uint ident = (uint)Util.GetWordUnsafe(addr, Runtime.WorkingEndian);
                ACMD_CMD_INFO info = Runtime.commandDictionary.FirstOrDefault(e => e.Identifier == ident);

                // If a command definition exists, use that info to deserialize.
                if (info != null)
                {
                    // Get command parameters and add the command to the event list.
                    c = new ACMDCommand(info);
                    for (int i = 0; i < info.ParamSpecifiers.Count; i++)
                    {
                        switch (info.ParamSpecifiers[i])
                        {
                            case 0:
                                c.parameters.Add(Util.GetWordUnsafe(0x04 + (addr + (i * 4)), Runtime.WorkingEndian));
                                break;
                            case 1:
                                c.parameters.Add(Util.GetFloatUnsafe(0x04 + (addr + (i * 4)), Runtime.WorkingEndian));
                                break;
                            case 2:
                                c.parameters.Add((decimal)Util.GetWordUnsafe(0x04 + (addr + (i * 4)), Runtime.WorkingEndian));
                                break;
                            default:
                                goto case 0;
                        }
                    }

                    _list.Add(c);
                    addr += c.CalcSize();
                }

                // If there is no command definition, this is unknown data.
                // Add the current word to the unk command and continue adding
                // until we hit a known command
                else
                {
                    _list.Add(new UnknownCommand() { ident = (uint)Util.GetWordUnsafe(addr, Runtime.WorkingEndian) });
                    addr += 0x04;
                }
            }

            // If we hit a script_end command, add it to the the Event List and terminate looping.
            if (Util.GetWordUnsafe(addr, Runtime.WorkingEndian) == Runtime._endingCommand.Identifier)
            {
                ACMD_CMD_INFO info = Runtime.commandDictionary.FirstOrDefault(e => e.Identifier == Runtime._endingCommand.Identifier);

                c = new ACMDCommand(info);
                _list.Add(c);
            }

            _list.Initialize();
            return _list;
        }

        /// <summary>
        /// Applies changes and then exports data to file.
        /// </summary>
        /// <param name="path"></param>    
        public void Export(string path)
        {
            Rebuild();

            if (_replSource == DataSource.Empty) return;

            _workingSource.Close();
            _workingSource = _replSource;
            _replSource = DataSource.Empty;


            DataSource src = _workingSource;
            byte[] tmp = new byte[Size];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = *(byte*)(src.Address + i);
            File.WriteAllBytes(path, tmp);
        }
        /// <summary>
        /// Returns an array of bytes representing this ACMDFile.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            DataSource src = _workingSource;
            byte[] tmp = new byte[Size];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = *(byte*)(src.Address + i);
            return tmp;
        }
        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();

            foreach (uint u in EventLists.Keys)
            {
                string label = "";
                AnimationHashPairs.TryGetValue(u, out label);
                if (string.IsNullOrEmpty(label))
                    label = $"{u:X8}";

                sb.Append(String.Format($"\n\n{EventLists.Keys.IndexOf(u):X}: [{label}]"));

                sb.Append("\n\tScript:{");
                if (EventLists[u] != null)
                    foreach (ACMDCommand cmd in EventLists[u])
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");
            }
            return sb.ToString();
        }
    }

    public enum ACMDType
    {
        Main = 0,
        GFX = 1,
        SFX = 2,
        Expression = 3,
        NONE = 255
    }
}
