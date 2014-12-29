using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace AnimCmd.Classes
{
    internal delegate Event CommandParser(VoidPtr addr);
    internal delegate string DictionaryLookup();

    public static class CommandFactory
    {
        private static List<CommandParser> _parsers = new List<CommandParser>();
        private static List<DictionaryLookup> _dictionaryDels = new List<DictionaryLookup>();
        
        static CommandFactory()
        {
            Delegate del;
            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
                if (t.IsSubclassOf(typeof(Event)))
                {
                    if ((del = Delegate.CreateDelegate(typeof(CommandParser), t, "TryParse", false, false)) != null)
                        _parsers.Add(del as CommandParser);
                    if ((del = Delegate.CreateDelegate(typeof(DictionaryLookup), t, "GetDictionaryName", false, false)) != null)
                        _dictionaryDels.Add(del as DictionaryLookup);
                }
        }

        public unsafe static Event FromAddress(VoidPtr addr)
        {
            Event n = null;
            foreach (CommandParser d in _parsers)
                if ((n = d(addr)) != null)
                    break;
            return n;
        }
        public unsafe static List<string> GetEventDictionary()
        {
            List<string> tmpDict = new List<string>();
            string s = "";
            foreach (DictionaryLookup d in _dictionaryDels)
                if (!String.IsNullOrEmpty(s = d()))
                    tmpDict.Add(s);
            return tmpDict;
        }
    }
}
