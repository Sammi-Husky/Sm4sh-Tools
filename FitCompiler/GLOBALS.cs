using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FitCompiler
{
    internal static class GLOBALS
    {
        public static string ACMD_PATH = Path.Combine("Script", "Animcmd");
        public static string MSC_PATH = Path.Combine("Script", "MSC");
        public static string ATTRS_PATH = Path.Combine("param", "fighter", "fighter_param.bin");
    }
}
