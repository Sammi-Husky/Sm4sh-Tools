using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPack
{
    public unsafe class Unpacker
    {
        DataSource _source;


        List<string> strings = new List<string>();
        List<int> stringOffsets = new List<int>();
        List<int> dataOffsets = new List<int>();
        List<int> sizes = new List<int>();

        public Unpacker(DataSource source)
        {
            _source = source;
        }

        public void Unpack(Endianness endian)
        {
            Unpack(endian,"Output");
        }
        public void Unpack(Endianness endian, string path)
        {
            if (endian == Endianness.big)
                UnpackBigEndian(path);
            else if(endian == Endianness.little)
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
                    File.WriteAllBytes(path+"/" + strings[i], _fileData);
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
    }
}
