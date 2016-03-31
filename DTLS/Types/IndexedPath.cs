using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTLS
{
    /// <summary>
    /// Indexable path object that automatically
    /// resizes itself when assigning a value to an out of range index.
    /// </summary>
    public class IndexedPath
    {
        public IndexedPath() { _parts = new string[0]; }
        public IndexedPath(string initVal) : this()
        {
            this[0] = initVal;
        }
        private string[] _parts;

        public string this[int index]
        {
            set
            {
                if (index >= _parts.Length)
                    Array.Resize(ref _parts, index + 2);
                else if (index < 0)
                    throw new IndexOutOfRangeException();

                _parts[index] = value;
                Array.Clear(_parts, index + 1, _parts.Length - (index + 1));
            }
        }
        public override string ToString()
        {
            return string.Join("", _parts);
        }
    }
}
