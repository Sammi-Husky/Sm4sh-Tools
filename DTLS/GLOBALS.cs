using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTLS
{
    internal static class GLOBALS
    {
        private static readonly string[] eu = { "eu_en", "eu_fr", "eu_sp", "eu_gr", "eu_it", "eu_ne", "eu_po", "eu_ru" };
        private static readonly string[] us = { "us_en", "us_fr", "us_sp" };
        private static readonly string[] jp = { "jp_jp" };
        public static readonly string[][] REGIONS = { us, eu, jp };
    }
}
