// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SALT.PARAMS
{
    public class ParamGroup : IParamCollection
    {
        public ParamGroup()
        {
            this.Values = new List<ParamEntry>();
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
                if (this.EntryCount > 0)
                    return this.Values.Count / this.EntryCount;
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
            data.Add(0x20);
            data.AddRange(BitConverter.GetBytes(this.EntryCount).Reverse());
            foreach (ParamEntry[] grp in this.Chunks)
            {
                foreach (ParamEntry ent in grp)
                    data.AddRange(ent.GetBytes());
            }

            return data.ToArray();
        }

        public void Chunk()
        {
            this.Chunks = (ParamEntry[][])this.Values.Chunk(this.EntryCount);
        }

        public void Add(ParamEntry ent)
        {
            this.Values.Add(ent);
        }

        public void Clear()
        {
            this.Values.Clear();
        }

        public ParamEntry[] this[int index]
        {
            get
            {
                if (index > this.Chunks.Length || index < 0)
                    throw new IndexOutOfRangeException();
                else
                    return this.Chunks[index];
            }
            set
            {
                if (index > this.Chunks.Length || index < 0)
                    throw new IndexOutOfRangeException();
                else
                    this.Chunks[index] = value;
            }
        }
    }
}
