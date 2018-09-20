using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTLS.Types;
using System.IO;
using ZLibNet;
using System.Diagnostics;

namespace DTLS
{
    public class ResourceManager
    {
        public ResourceManager(string[] dt, LSFile ls)
        {
            Files = new Dictionary<string, Tuple<LSEntry, ResourceEntry>>();
            DTFiles = dt;
            LS = ls;
        }

        public string[] DTFiles { get; set; }
        public RFFile RF { get; set; }
        public LSFile LS { get; set; }
        public int RegionCode { get; set; }
        public Dictionary<string, Tuple<LSEntry, ResourceEntry>> Files { get; set; }

        public bool InitializePartition(RFFile rf, string partition)
        {
            Files.Clear();
            RegionCode = rf.RegionCode;
            LSEntry lsentry = null;
            IndexedPath path = new IndexedPath($"data{RF.Locale}/");
            foreach (ResourceEntry rsobj in RF.ResourceEntries)
            {
                if (rsobj == null)
                    continue;

                path[rsobj.FolderDepth] = rsobj.EntryString;

                if (rsobj.Packed)
                    lsentry = LS.TryGetValue(path.ToString() + "packed");

                if (lsentry != null)
                {
                    var tpl = new Tuple<LSEntry, ResourceEntry>(lsentry, rsobj);
                    if (rsobj.Packed)
                        Files.Add(path.ToString() + "packed", tpl);
                    else
                        Files.Add(path.ToString(), tpl);
                }
            }
            return true;
        }
        public bool InitializePartition(string partition)
        {
            LSEntry resource = null;
            if ((resource = LS.TryGetValue(partition)) == null)
                return false;

            File.WriteAllBytes(partition,
                GetFile(resource.DTOffset, resource.Size, resource.DTIndex));

            var rf = new RFFile(partition);
            if (partition.Contains("("))
                rf.Locale = partition.Substring(partition.IndexOf("("));

            return InitializePartition(rf, partition);
        }
        public void Unpack(string partition, string outfolder)
        {
            if (!InitializePartition(partition))
                return;
#if DEBUG
            Stopwatch s = new Stopwatch();
            s.Start();
#endif
            if (!Directory.Exists(outfolder))
                Directory.CreateDirectory(outfolder);

            foreach (var str in Files.Keys)
            {
                if (!str.EndsWith("/"))
                {
                    var dir = Path.GetDirectoryName($"{outfolder}/{str}");
                    var tpl = Files[str];

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    Console.WriteLine($"{outfolder}/{str}");

                    var filebytes = GetFile(str);
                    if (tpl.Item2.CmpSize != tpl.Item2.DecSize && !tpl.Item2.Packed)
                        filebytes = Util.DeCompress(filebytes);

                    File.WriteAllBytes($"{outfolder}/{str}", filebytes);
                }
            }
#if DEBUG
            s.Stop();
            Console.WriteLine("Elapsed Time: " + s.Elapsed);
            Console.Write("Press any key to continue");
            Console.ReadLine();
#endif
        }
        public void UnpackAll(string outfolder)
        {
            Unpack("resource", outfolder);
            foreach (string lang in GLOBALS.REGIONS[RegionCode])
                Unpack($"resource({lang})", outfolder);
        }
        public void PatchAll(string root)
        {
            PatchPartition(root, "resource");
            foreach (string lang in GLOBALS.REGIONS[RegionCode])
                PatchPartition(root, $"resource({lang})");
        }
        public byte[] GetFile(string path)
        {
            Tuple<LSEntry, ResourceEntry> tpl = null;
            Files.TryGetValue(path, out tpl);
            if (tpl == null)
                return null;

            uint off = tpl.Item1.DTOffset + tpl.Item2.OffInPack;
            int len = tpl.Item2.CmpSize;
            return GetFile(off, len, tpl.Item1.DTIndex);
        }
        public byte[] GetFile(uint off, int len, int DTIndex)
        {
            using (var strm = new FileStream(DTFiles[DTIndex], FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(strm))
                {
                    reader.BaseStream.Seek(off, SeekOrigin.Begin);
                    return reader.ReadBytes(len);
                }
            }
        }

        public void BuildPartitions(string folder)
        {
            if (DTFiles == null)
                return;

            FileStream dtstrm = File.Create("dt_rebuild");
            string tmpfile = Path.GetTempFileName();
            FileStream packstrm = File.Create(tmpfile);

            foreach (string str in Directory.EnumerateDirectories(folder))
            {
                if (!str.Contains("data"))
                    return;

                string partition = "resource";
                if (str.Contains("("))
                    partition += str.Substring(str.IndexOf("("));

                var rf = new RFFile();

                LSEntry curPacked = null;
                int pad = 0;
                string packKey = "";
                foreach (string key in Files.Keys)
                {
                    Console.WriteLine(key);
                    var tpl = Files[key];
                    if (tpl.Item2.Packed)
                    {
                        if (curPacked != null)
                        {
                            int aligned = (int)packstrm.Position.RoundUp(0x10) + 0x60;
                            while (packstrm.Position < aligned)
                                packstrm.WriteByte(0xBB);
                            curPacked.Size = (int)packstrm.Length;
                            LS.TrySetValue(packKey, curPacked);

                            packstrm.Position = 0;
                            packstrm.CopyTo(dtstrm);
                            packstrm.Close();
                            packstrm = File.Open(tmpfile, FileMode.Truncate);
                        }
                        curPacked = tpl.Item1;
                        packKey = key;
                        curPacked.DTOffset = (uint)dtstrm.Position;
                        var data = File.ReadAllBytes($"{folder}/{key}");
                        for (pad = 0; pad < data.Length && data[pad] == 0xCC; pad++)
                            packstrm.WriteByte(0xCC);
                    }
                    else if (!tpl.Item2.EntryString.EndsWith("/"))
                    {
                        var filedata = File.ReadAllBytes($"{folder}/{key}");
                        var cmp = Util.Compress(filedata);
                        ResourceEntry rsobj = tpl.Item2;
                        int rIndex = RF.ResourceEntries.IndexOf(rsobj);

                        rsobj.OffInPack = (uint)packstrm.Position;
                        rsobj.CmpSize = cmp.Length;
                        rsobj.DecSize = filedata.Length;
                        RF[rIndex] = rsobj;
                        packstrm.Write(cmp, 0, cmp.Length);

                        int aligned = (int)packstrm.Position.RoundUp(0x10) + 0x20;
                        while (packstrm.Position % 0x10 > 0)
                            packstrm.WriteByte(0xCC);
                    }
                }
                if (curPacked != null)
                {
                    curPacked.Size = (int)packstrm.Length;
                    LS.TrySetValue(packKey, curPacked);
                    packstrm.Position = 0;
                    packstrm.CopyTo(dtstrm);
                }
                RF.UpdateEntries();


                byte[] full = RF.GetBytes();
                var resource = LS.TryGetValue(partition);
                resource.DTOffset = (uint)dtstrm.Position;
                dtstrm.Write(full, 0, full.Length);
                resource.Size = full.Length;
                LS.TrySetValue(partition, resource);
                LS.UpdateEntries();
                dtstrm.Close();
                packstrm.Close();
            }
        }
        public void PatchPartition(string folder, string partition)
        {
            if (!InitializePartition(partition))
                return;

            var resource = LS.TryGetValue(partition);
            if (resource == null) return;

            Console.WriteLine($"------{partition}------");
            foreach (string key in Files.Keys)
            {
                string filepath = $"{folder}/{key}";
                if (File.Exists(filepath))
                {
                    Console.WriteLine($"Patch: {key}");
                    var tpl = Files[key];
                    var rsobj = tpl.Item2;

                    int index = RF.ResourceEntries.IndexOf(rsobj);
                    byte[] data = File.ReadAllBytes(filepath);
                    byte[] cmp = Util.Compress(data);
                    using (FileStream stream = File.Open(DTFiles[tpl.Item1.DTIndex], FileMode.Open))
                    {
                        // Overwrite existing data
                        stream.Seek(tpl.Item1.DTOffset + rsobj.OffInPack, SeekOrigin.Begin);
                        for (int i = 0; i < rsobj.CmpSize; i++)
                            stream.WriteByte(0xCC);

                        // Get how much space we have to work with
                        stream.Seek(tpl.Item1.DTOffset + rsobj.OffInPack, SeekOrigin.Begin);
                        int len = 0;
                        while (stream.ReadByte() == 0xCC)
                            len++;

                        // If the new file is within range, write the new data
                        stream.Seek(tpl.Item1.DTOffset + rsobj.OffInPack, SeekOrigin.Begin);
                        if (cmp.Length <= len)
                            stream.Write(cmp, 0, cmp.Length);
                    }

                    rsobj.DecSize = data.Length;
                    rsobj.CmpSize = cmp.Length;
                    RF[index] = rsobj;
                }
            }
            RF.UpdateEntries();
            RF.WorkingSource.Close();
            byte[] resbytes = RF.GetBytes();
            using (var stream = File.Open(DTFiles[resource.DTIndex], FileMode.Open))
            {
                stream.Seek(resource.DTOffset, SeekOrigin.Begin);
                stream.Write(resbytes, 0, resbytes.Length);
            }
            resource.Size = resbytes.Length;
            LS.TrySetValue(partition, resource);
            LS.UpdateEntries();
        }
    }
}
