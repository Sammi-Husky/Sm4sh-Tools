using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPack
{
    public unsafe class PACKManager
    {
        DataSource _source;
        Endianness _endian;

        List<string> strings = new List<string>();
        List<int> stringOffsets = new List<int>();
        List<int> dataOffsets = new List<int>();
        List<int> sizes = new List<int>();

        public PACKManager(string path)
        {
            _source = new DataSource(FileMap.FromFile(path));
            string magic = new String((sbyte*)_source.Address);

            if (magic == "PACK")
                _endian = Endianness.little;
            else if (magic == "KCAP")
                _endian = Endianness.big;
            else
                Console.WriteLine("Not a valid PACK file");
        }
        public PACKManager(Endianness endian)
        {
            _endian = endian;
        }

        public void Unpack(string path)
        {
            if (_endian == Endianness.big)
                UnpackBigEndian(path);
            else if (_endian == Endianness.little)
                UnpackLittleEndian(path);
        }
        private void UnpackBigEndian(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            int count = *(bint*)(_source.Address + 0x08);

            for (int i = 0; i < count; i++)
                stringOffsets.Add(*(bint*)(_source.Address + (i * 0x04) + 0x10));
            for (int i = 0; i < count; i++)
                dataOffsets.Add(*(bint*)(_source.Address + (stringOffsets.Count * 4) + (i * 4) + 0x10));
            for (int i = 0; i < count; i++)
                sizes.Add(*(bint*)(_source.Address + (stringOffsets.Count * 4) + (dataOffsets.Count * 4) + (i * 4) + 0x10));


            foreach (int off in stringOffsets)
                strings.Add(new String((sbyte*)_source.Address + off));


            for (int i = 0; i < count; i++)
            {
                byte[] _fileData = new byte[sizes[i]];

                using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)(_source.Address + dataOffsets[i]), sizes[i]))
                    stream.Read(_fileData, 0, (int)stream.Length);

                try
                {
                    File.WriteAllBytes(path + "/" + strings[i], _fileData);
                }

                catch (Exception x) { Console.WriteLine(x.Message); }
            }
        }
        private void UnpackLittleEndian(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            int count = *(bint*)(_source.Address + 0x08);

            for (int i = 0; i < count; i++)
                stringOffsets.Add(*(bint*)(_source.Address + (i * 0x04) + 0x10));
            for (int i = 0; i < count; i++)
                dataOffsets.Add(*(bint*)(_source.Address + (stringOffsets.Count * 4) + (i * 4) + 0x10));
            for (int i = 0; i < count; i++)
                sizes.Add(*(bint*)(_source.Address + (stringOffsets.Count * 4) + (dataOffsets.Count * 4) + (i * 4) + 0x10));


            foreach (int off in stringOffsets)
                strings.Add(new String((sbyte*)_source.Address + off));


            for (int i = 0; i < count; i++)
            {
                byte[] _fileData = new byte[sizes[i]];

                using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)(_source.Address + dataOffsets[i]), sizes[i]))
                    stream.Read(_fileData, 0, (int)stream.Length);

                try
                {
                    File.WriteAllBytes(path + "/" + strings[i], _fileData);
                }

                catch (Exception x) { Console.WriteLine(x.Message); }
            }
        }

        public void Pack(string folder)
        {
            using (FileStream dest = new FileStream($"repack.pac", FileMode.Create, FileAccess.ReadWrite))
            {
                strings = Directory.EnumerateFiles(folder).ToList();
                if (_endian == Endianness.big)
                    dest.Write(Encoding.ASCII.GetBytes("KCAP"), 0, 4);
                else
                    dest.Write(Encoding.ASCII.GetBytes("PACK"), 0, 4);
                dest.Write(new byte[4], 0, 4);
                dest.Write(BitConverter.GetBytes(_endian == Endianness.big ? strings.Count.Reverse() : strings.Count), 0, 4);
                dest.Write(new byte[4], 0, 4);

                // Write entire offset section first, we'll fill it in later
                dest.Write(new byte[strings.Count * 0x0C], 0, strings.Count * 0x0C);

                foreach (string s in strings)
                {
                    FileInfo file = new FileInfo(s);
                    sizes.Add((int)file.Length);
                    string NameWithExt = Path.GetFileName(s);
                    stringOffsets.Add((int)dest.Position);
                    dest.Write(Encoding.ASCII.GetBytes(NameWithExt + '\0'), 0, NameWithExt.Length + 1);
                }
                while (dest.Position % 0x10 != 0)
                    dest.WriteByte(0);
                for (int i = 0; i < strings.Count; i++)
                {
                    using (FileStream source = new FileStream(strings[i], FileMode.Open, FileAccess.Read))
                    {
                        dataOffsets.Add((int)dest.Position);
                        source.CopyTo(dest);
                    }
                    if (i != strings.Count - 1)
                        while (dest.Position % 0x10 != 0)
                            dest.WriteByte(0);
                }

                dest.Seek(0x10, SeekOrigin.Begin);
                for (int i = 0; i < strings.Count; i++)
                    dest.Write(BitConverter.GetBytes(_endian == Endianness.big ? stringOffsets[i].Reverse() : stringOffsets[i]), 0, 4);
                for (int i = 0; i < strings.Count; i++)
                    dest.Write(BitConverter.GetBytes(_endian == Endianness.big ? dataOffsets[i].Reverse() : dataOffsets[i]), 0, 4);
                for (int i = 0; i < strings.Count; i++)
                    dest.Write(BitConverter.GetBytes(_endian == Endianness.big ? sizes[i].Reverse() : sizes[i]), 0, 4);

                dest.Close();
            }
        }
    }
}
