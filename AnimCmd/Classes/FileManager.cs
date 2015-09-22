using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sm4shCommand.Classes;
using System.IO;
using System.Windows.Forms;

namespace Sm4shCommand.Classes
{
    unsafe static class FileManager
    {
        public static ACMDFile OpenFile(string Filepath)
        {
            DataSource source = new DataSource(FileMap.FromFile(Filepath));

            if (*(byte*)(source.Address + 0x04) == 0x02)
                Runtime.WorkingEndian = Endianness.Little;
            else if ((*(byte*)(source.Address + 0x04) == 0x00))
                Runtime.WorkingEndian = Endianness.Big;
            else
            {
                MessageBox.Show("Could not determine endianness of file. Unsupported file version or file header is corrupt.");
                return null;
            }

            return new ACMDFile(source, Runtime.WorkingEndian);
        }
        public static Fighter OpenFighter(string dirPath)
        {
            Fighter f = new Fighter();
            try
            {

                f.Main = OpenFile(dirPath + "/game.bin");
                f.GFX = OpenFile(dirPath + "/effect.bin");
                f.SFX = OpenFile(dirPath + "/sound.bin");
                f.Expression = OpenFile(dirPath + "/expression.bin");

                f.Main.Type = ACMDType.Main;
                f.GFX.Type = ACMDType.GFX;
                f.SFX.Type = ACMDType.SFX;
                f.Expression.Type = ACMDType.Expression;

                f.MotionTable = ParseMTable(new DataSource(FileMap.FromFile(dirPath + "/motion.mtable")), Runtime.WorkingEndian);
            }
            catch (FileNotFoundException x) { MessageBox.Show(x.Message); return null; }

            Runtime.isRoot = true;
            Runtime.rootPath = dirPath;
            Runtime.Instance.Text = String.Format("Main Form - {0}", dirPath);
            return f;
        }
        public static MTable ParseMTable(DataSource source, Endianness endian)
        {
            List<uint> CRCTable = new List<uint>();

            for (int i = 0; i < source.Length; i += 4)
                //if((uint)Util.GetWordUnsafe((source.Address + i), endian) != 0)
                CRCTable.Add((uint)Util.GetWordUnsafe((source.Address + i), endian));

            return new MTable(CRCTable, endian);
        }
    }
}
