// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SALT.PARAMS
{
    public class ParamGroup : ParamEntry, IParamCollection
    {
        public ParamGroup(object value, ParamType type) : base(value, ParamType.group)
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
        public ParamList[] Chunks { get; set; }
        public List<ParamEntry> Values { get; set; }

        public byte[] GetBytes()
        {
            List<byte> data = new List<byte>();
            data.Add(0x20);
            data.AddRange(BitConverter.GetBytes(this.EntryCount).Reverse());
            foreach (ParamList grp in this.Chunks)
            {
                foreach (ParamEntry ent in grp.Values)
                    data.AddRange(ent.GetBytes());
            }

            return data.ToArray();
        }

        public void Chunk()
        {
            var _chunks = new List<ParamList>();
            foreach (var chunk in this.Values.Chunk(this.EntryCount))
            {
                _chunks.Add(new ParamList(chunk));
            }
            this.Chunks = _chunks.ToArray();
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

        public ParamList this[int index]
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
