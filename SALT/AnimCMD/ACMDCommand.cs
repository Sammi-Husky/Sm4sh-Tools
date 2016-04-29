using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALT.AnimCMD
{
    public class ACMDCommand
    {
        public ACMDCommand(uint CRC)
        {

        }
        public uint CRC { get; set; }
        public bool Dirty { get; set; }

        public byte[] GetBytes()
        {
            return null;
        }
        public int CalcSize()
        {
            return 0;
        }
    }
}
