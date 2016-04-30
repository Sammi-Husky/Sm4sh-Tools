using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.PARAMS
{
    public class ParamList : IParamCollection
    {
        public ParamList() { Values = new List<ParamEntry>(); }
        public List<ParamEntry> Values { get; set; }
        public byte[] GetBytes()
        {
            List<byte> data = new List<byte>();
            foreach (ParamEntry ent in Values)
                data.AddRange(ent.GetBytes());

            return data.ToArray();
        }
        public void Add(ParamEntry ent)
        {
            Values.Add(ent);
        }
        public void Clear()
        {
            Values.Clear();
        }
    }
}
