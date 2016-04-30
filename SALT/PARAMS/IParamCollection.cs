using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.PARAMS
{
    public interface IParamCollection
    {
        byte[] GetBytes();
        void Add(ParamEntry value);
        void Clear();
        List<ParamEntry> Values { get; set; }
    }
}
