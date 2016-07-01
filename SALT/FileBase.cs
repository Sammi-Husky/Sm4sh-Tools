using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SALT
{
    public abstract class BaseFile
    {
        public int Size { get { return this.CalcSize(); } }

        public abstract void Export(string path);
        public abstract int CalcSize();
        public abstract byte[] GetBytes();
    }
}
