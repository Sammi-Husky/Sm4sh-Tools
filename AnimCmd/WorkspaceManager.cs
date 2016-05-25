using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using SALT.Scripting;
using SALT.Scripting.AnimCMD;
using SALT.Scripting.MSC;
using Sm4shCommand.Nodes;
using System.Xml;

namespace Sm4shCommand
{
    unsafe class WorkspaceManager
    {
        public WorkspaceManager() { Projects = new SortedList<string, Project>(); }
        public SortedList<string, Project> Projects { get; set; }
        public string WorkspaceRoot { get; set; }

        public void OpenProject(string filepath)
        {
            Runtime.Instance.FileTree.Nodes.Clear();
            var p = new Project(filepath);
            Projects.Add(p.ProjName, p);
            Runtime.Instance.FileTree.Nodes.Add(p);
        }
        public void SaveProject(string project, string outPath)
        {
            foreach (Project proj in Projects.Values)
            {
                proj.ProjPath = $"{outPath + proj.Name}.fitproj";
                var path = proj.ProjRoot + "/ACMD";
                Directory.CreateDirectory(path);
                foreach (var acmd in proj.ACMD_FILES)
                {
                    File.WriteAllBytes(
                        $"{path}/{acmd.Key}.bin",
                        acmd.Value.GetBytes(Runtime.WorkingEndian));
                }
                using (var stream = File.Create($"{path}/Motion.mtable"))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        foreach (uint u in proj.MotionTable)
                            writer.Write(u);
                    }
                }
                path = proj.ProjRoot + "/MSC";
                Directory.CreateDirectory(path);
                foreach (var msc in proj.MSC_FILES)
                {
                    File.WriteAllBytes(
                        $"{path}/{msc.Key}.mscsb",
                        msc.Value.GetBytes(Endianness.Little));
                }
                path = proj.ProjRoot + "/ANIM";
                Directory.CreateDirectory(path);
                foreach (var anim in proj.ANIM_FILES)
                    File.Copy(anim, $"{path}/{Path.GetFileName(anim)}");
                proj.ProjFile = proj.WriteFitproj(proj.ProjPath);
            }
        }
    }

    public unsafe class Project : TreeNode
    {
        public ContextMenuStrip _menu;
        public Project(string filepath)
        {
            ACMD_FILES = new SortedList<string, IScriptCollection>();
            MSC_FILES = new SortedList<string, IScriptCollection>();
            AnimationHashes = new Dictionary<uint, string>();
            ANIM_FILES = new List<string>();
            MotionTable = new List<uint>();

            _menu = new ContextMenuStrip();
            _menu.Items.Add(new ToolStripMenuItem("Import Script", null, ImportScript));
            ContextMenuStrip = _menu;

            this.Nodes.Add("ACMD");
            this.Nodes.Add("MSC");
            this.Nodes.Add("Attributes");
            this.Nodes.Add("Param_vl");

            ProjFile = ReadProject(filepath);
            PopulateACMD();
            PopulateMSC();
        }

        // Project Nodes
        public TreeNode ACMDNode { get { return this.Nodes[0]; } set { this.Nodes[0] = value; } }
        public TreeNode MSCNode { get { return this.Nodes[1]; } set { this.Nodes[1] = value; } }
        public TreeNode ATTRNode { get { return this.Nodes[2]; } set { this.Nodes[2] = value; } }
        public TreeNode PARAMNode { get { return this.Nodes[3]; } set { this.Nodes[3] = value; } }

        // Project Properties
        public XmlDocument ProjFile { get; set; }
        public string ProjPath { get; set; }
        public string ProjRoot { get { return Path.GetDirectoryName(ProjPath); } }
        public string ProjName { get; set; }
        public string ToolVer { get; set; }
        public string GameVer { get; set; }
        public string Platform { get; set; }

        public SortedList<string, IScriptCollection> ACMD_FILES { get; set; }
        public SortedList<string, IScriptCollection> MSC_FILES { get; set; }
        public List<string> ANIM_FILES { get; set; }
        public List<uint> MotionTable { get; set; }

        public Dictionary<uint, string> AnimationHashes { get; set; }

        //public IParamCollection Attributes { get; set; }
        //public IParamCollection Fighter_Param_vl { get; set; }

        private void ParseAnim(string path)
        {
            if (path.EndsWith(".pac"))
            {
                byte[] filebytes = File.ReadAllBytes(path);
                int count = (int)Util.GetWord(filebytes, 8, Endianness.Big);

                for (int i = 0; i < count; i++)
                {
                    uint off = (uint)Util.GetWord(filebytes, 0x10 + (i * 4), Endianness.Big);
                    string FileName = Util.GetString(filebytes, off, Endianness.Big);
                    string AnimName = Regex.Match(FileName, @"(.*)([A-Z])([0-9][0-9])(.*)\.omo").Groups[4].ToString();
                    if (string.IsNullOrEmpty(AnimName))
                        continue;

                    AddAnimHash(AnimName);
                    AddAnimHash(AnimName + "_C2");
                    AddAnimHash(AnimName + "_C3");
                    AddAnimHash(AnimName + "L");
                    AddAnimHash(AnimName + "R");


                    if (AnimName.EndsWith("s4s", StringComparison.InvariantCultureIgnoreCase) ||
                       AnimName.EndsWith("s3s", StringComparison.InvariantCultureIgnoreCase))
                        AddAnimHash(AnimName.Substring(0, AnimName.Length - 1));
                }
            }
            else if (path.EndsWith(".bch"))
            {
                DataSource src = new DataSource(FileMap.FromFile(path));
                int off = *(int*)(src.Address + 0x0C);
                VoidPtr addr = src.Address + off;
                while (*(byte*)addr != 0)
                {
                    var tmp = new string((sbyte*)addr);
                    string AnimName = Regex.Match(tmp, @"(.*)([A-Z])([0-9][0-9])(.*)").Groups[4].ToString();
                    if (string.IsNullOrEmpty(AnimName))
                    {
                        addr += tmp.Length + 1;
                        continue;
                    }

                    AddAnimHash(AnimName);
                    AddAnimHash(AnimName + "_C2");
                    AddAnimHash(AnimName + "_C3");
                    AddAnimHash(AnimName + "L");
                    AddAnimHash(AnimName + "R");


                    if (AnimName.EndsWith("s4s", StringComparison.InvariantCultureIgnoreCase) ||
                       AnimName.EndsWith("s3s", StringComparison.InvariantCultureIgnoreCase))
                        AddAnimHash(AnimName.Substring(0, AnimName.Length - 1));

                    addr += tmp.Length + 1;
                }
            }
        }
        private void AddAnimHash(string name)
        {
            uint crc = Crc32.Compute(name.ToLower());
            if (AnimationHashes.ContainsValue(name) || AnimationHashes.ContainsKey(crc))
                return;

            AnimationHashes.Add(crc, name);
        }
        private bool OpenFile(string Filepath)
        {
            bool handled = false;
            if (Filepath.EndsWith(".bin"))
            {
                DataSource source = new DataSource(FileMap.FromFile(Filepath));
                if (*(buint*)source.Address == 0x41434D44) // ACMD
                {
                    if (*(byte*)(source.Address + 0x04) == 0x02)
                        Runtime.WorkingEndian = Endianness.Little;
                    else if ((*(byte*)(source.Address + 0x04) == 0x00))
                        Runtime.WorkingEndian = Endianness.Big;
                    else
                        handled = false;

                    ACMD_FILES.Add(Path.GetFileNameWithoutExtension(Filepath), new ACMDFile(source));
                    handled = true;
                }
                else if ((*(buint*)source.Address) == 0xFFFF0000) // PARAM
                {
                    throw new NotImplementedException();
                    //return true;
                }
            }
            else if (Filepath.EndsWith(".mscsb")) // MSC
            {
                MSC_FILES.Add(Path.GetFileNameWithoutExtension(Filepath), new MSCFile(Filepath));
                handled = true;
            }
            else if (Filepath.EndsWith(".mtable"))
            {
                ParseMTable(Filepath, Runtime.WorkingEndian);
            }

            return handled;
        }
        private void ParseMTable(string filepath, Endianness endian)
        {
            MotionTable.Clear();
            using (var stream = File.Open(filepath, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position != stream.Length)
                        MotionTable.Add(reader.ReadBuint32());
                }
            }
        }
        private XmlDocument ReadProject(string filepath)
        {
            ProjPath = filepath;
            var proj = new XmlDocument();
            proj.Load(filepath);

            var node = proj.SelectSingleNode("//Project");
            this.Text = ProjName = node.Attributes["Name"].Value;
            this.ToolVer = node.Attributes["ToolVer"].Value;
            this.GameVer = node.Attributes["GameVer"].Value;
            this.Platform = node.Attributes["Platform"].Value;

            node = proj.SelectSingleNode("//Project/ACMD");
            foreach (XmlNode child in node.ChildNodes)
                OpenFile(Path.GetDirectoryName(filepath) + $"/{child.Attributes["include"].Value}");
            node = proj.SelectSingleNode("//Project/MSC");
            foreach (XmlNode child in node.ChildNodes)
                OpenFile(Path.GetDirectoryName(filepath) + $"/{child.Attributes["include"].Value}");
            node = proj.SelectSingleNode("//Project/ANIM");
            foreach (XmlNode child in node.ChildNodes)
            {
                ANIM_FILES.Add(Path.GetDirectoryName(filepath) + $"/{child.Attributes["include"].Value}");
                ParseAnim(Path.GetDirectoryName(filepath) + $"/{child.Attributes["include"].Value}");
            }

            return proj;
        }
        public XmlDocument WriteFitproj(string filepath)
        {
            var writer = XmlWriter.Create(filepath, new XmlWriterSettings() { Indent = true, IndentChars = "\t" });
            writer.WriteStartDocument();
            writer.WriteStartElement("Project");
            writer.WriteAttributeString("Name", ProjName);
            writer.WriteAttributeString("ToolVer", ToolVer);
            writer.WriteAttributeString("GameVer", GameVer);
            writer.WriteAttributeString("Platform", Platform);

            writer.WriteStartElement("ACMD");
            foreach (var acmd in ACMD_FILES)
            {
                writer.WriteStartElement("Import");
                writer.WriteAttributeString("include", $"/ACMD/{acmd.Key}.bin");
                writer.WriteEndElement();
            }
            writer.WriteStartElement("Import");
            writer.WriteAttributeString("include", $"/ACMD/Motion.mtable");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("MSC");
            foreach (var msc in MSC_FILES)
            {
                writer.WriteStartElement("Import");
                writer.WriteAttributeString("include", $"/MSC/{msc.Key}.mscsb");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("ANIM");
            foreach (var anim in ANIM_FILES)
            {
                writer.WriteStartElement("Import");
                writer.WriteAttributeString("include", $"/ANIM/{anim}.bin");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
            var doc = new XmlDocument();
            doc.Load(filepath);
            return doc;
        }

        public void PopulateACMD()
        {
            ACMDNode.Nodes.Clear();
            foreach (uint u in MotionTable)
            {
                ScriptNode snode = new ScriptNode($"{MotionTable.IndexOf(u)} - {u.ToString("X8")}");

                if (AnimationHashes.ContainsKey(u))
                    snode.Text = $"{MotionTable.IndexOf(u)} - {AnimationHashes[u]}";

                if (ACMD_FILES.ContainsKey("game"))
                    if (ACMD_FILES["game"].Scripts.ContainsKey(u))
                        snode.Scripts.Add("Game", ACMD_FILES["game"].Scripts[u]);
                if (ACMD_FILES.ContainsKey("effect"))
                    if (ACMD_FILES["effect"].Scripts.ContainsKey(u))
                        snode.Scripts.Add("Effect", ACMD_FILES["effect"].Scripts[u]);
                if (ACMD_FILES.ContainsKey("sound"))
                    if (ACMD_FILES["sound"].Scripts.ContainsKey(u))
                        snode.Scripts.Add("Sound", ACMD_FILES["sound"].Scripts[u]);
                if (ACMD_FILES.ContainsKey("expression"))
                    if (ACMD_FILES["expression"].Scripts.ContainsKey(u))
                        snode.Scripts.Add("Expression", ACMD_FILES["expression"].Scripts[u]);

                ACMDNode.Nodes.Add(snode);
            }
        }
        public void PopulateMSC()
        {
            MSCNode.Nodes.Clear();
            var file = MSC_FILES.ElementAt(0).Value;
            foreach (var scr in file.Scripts)
            {
                var snode = new ScriptNode($"Script_{file.Scripts.IndexOfValue(scr.Value)}");
                snode.Scripts.Add("Script", scr.Value);
                MSCNode.Nodes.Add(snode);
            }
        }
        private void ImportScript(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "ACMD Binary (.bin)|*.bin|" +
                             "MSC Binary (.mscsb)|*.mscsb|" +
                             "All Files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                    OpenFile(dlg.FileName);
            }
        }
    }
    public enum ProjType
    {
        Fighter,
        Weapon
    }
}
