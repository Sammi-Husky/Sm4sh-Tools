using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SALT.Scripting.AnimCMD;
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = Console.ReadLine();
            ACMDFile f = new ACMDFile(path);
        }
    }
}
