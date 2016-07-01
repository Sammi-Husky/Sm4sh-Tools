// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace SALT.PARAMS
{
    public class ParamList : IParamCollection
    {
        public ParamList() { this.Values = new List<ParamEntry>(); }
        public List<ParamEntry> Values { get; set; }
        public byte[] GetBytes()
        {
            List<byte> data = new List<byte>();
            foreach (ParamEntry ent in this.Values)
                data.AddRange(ent.GetBytes());

            return data.ToArray();
        }

        public void Add(ParamEntry ent)
        {
            this.Values.Add(ent);
        }

        public void Clear()
        {
            this.Values.Clear();
        }
        public int CalcSize() { return Values.Sum(x => x.Size); }

    }
}
