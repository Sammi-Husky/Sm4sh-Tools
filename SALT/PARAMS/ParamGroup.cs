using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.PARAMS
{
    public class ParamGroup : IParamCollection
    {
        public ParamGroup()
        {
            Values = new List<ParamEntry>();
        }
        /// <summary>
        /// Number of structures this group contains.
        /// </summary>
        public int EntryCount { get; set; }
        /// <summary>
        /// Number of parameters each structure contains.
        /// </summary>
        public int EntrySize
        {
            get
            {
                if (EntryCount > 0)
                    return Values.Count / EntryCount;
                else
                    return 1;
            }
        }
        /// <summary>
        /// Param structures.
        /// </summary>
        public ParamEntry[][] Chunks { get; set; }
        public List<ParamEntry> Values { get; set; }

        public byte[] GetBytes()
        {
            List<byte> data = new List<byte>();
            foreach (ParamEntry[] grp in Chunks)
                foreach (ParamEntry ent in grp)
                    data.AddRange(ent.GetBytes());

            return data.ToArray();
        }
        public void Chunk()
        {
            Chunks = (ParamEntry[][])Values.Chunk(EntryCount);
        }
        public void Add(ParamEntry ent)
        {
            Values.Add(ent);
        }
        public void Clear()
        {
            Values.Clear();
        }

        public ParamEntry[] this[int index]
        {
            get
            {
                if (index > Chunks.Length || index < 0)
                    throw new IndexOutOfRangeException();
                else
                    return Chunks[index];
            }
            set
            {
                if (index > Chunks.Length || index < 0)
                    throw new IndexOutOfRangeException();
                else
                    Chunks[index] = value;
            }
        }
    }
}
