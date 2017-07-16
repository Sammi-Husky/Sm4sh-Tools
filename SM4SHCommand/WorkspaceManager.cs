using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using SALT.Moveset;
using SALT.Moveset.AnimCMD;
using SALT.Moveset.MSC;
using Sm4shCommand.Nodes;
using System.Xml;
using SALT.PARAMS;

namespace Sm4shCommand
{
    unsafe class WorkspaceManager
    {
        public WorkspaceManager() { Projects = new SortedList<string, Project>(); }
        public SortedList<string, Project> Projects { get; set; }
        public string WorkspaceRoot { get; set; }

        public void OpenProject(string filepath)
        {
            Projects.Clear();
            Runtime.Instance.FileTree.Nodes.Clear();
            var p = new Project(filepath);
            Projects.Add(p.ProjName, p);
            Runtime.Instance.FileTree.Nodes.Add(p);
        }
        public void OpenFile(string filepath)
        {
            if (filepath.EndsWith(".bin"))
            {
                DataSource source = new DataSource(FileMap.FromFile(filepath));
                if (*(buint*)source.Address == 0x41434D44) // ACMD
                {
                    if (*(byte*)(source.Address + 0x04) == 0x02)
                        Runtime.WorkingEndian = Endianness.Little;
                    else if ((*(byte*)(source.Address + 0x04) == 0x00))
                        Runtime.WorkingEndian = Endianness.Big;

                    var f = new ACMDFile(source);
                    var node = new TreeNode("ACMD");
                    foreach (var keypair in f.Scripts)
                        node.Nodes.Add(new ScriptNode(keypair.Key, $"{keypair.Key:X8}", keypair.Value));
                    Runtime.Instance.FileTree.Nodes.Add(node);
                }
            }
            else if (filepath.EndsWith(".mscsb")) // MSC
            {
                var f = new MSCFile(filepath);
                var node = new TreeNode("MSC");

                for (int i = 0; i < f.Scripts.Count; i++)
                    node.Nodes.Add(new ScriptNode((uint)i, $"{i:X8}", f.Scripts.Values[i]));
                Runtime.Instance.FileTree.Nodes.Add(node);
            }
        }
        public void ImportProject(string filepath)
        {
            var p = new Project(filepath);
            if (Projects.ContainsKey(p.ProjName))
                throw new IOException("A project with this name already exists.");
            Projects.Add(p.ProjName, p);
            Runtime.Instance.FileTree.Nodes.Add(p);
        }
    }

    public unsafe class Project : TreeNode
    {
        public ContextMenuStrip _menu;
        public Project()
        {
            ACMD_FILES = new SortedList<string, IScriptCollection>();
            MSC_FILES = new SortedList<string, IScriptCollection>();
            AnimationHashes = new Dictionary<uint, string>();
            ANIM_FILES = new List<string>();

            _menu = new ContextMenuStrip();
            _menu.Items.Add(new ToolStripMenuItem("Import Script", null, ImportScript));
            ContextMenuStrip = _menu;

            this.Nodes.Add("ACMD");
            this.Nodes.Add("MSC");
            this.Nodes.Add("Attributes");
            this.Nodes.Add("Param_vl");
        }
        public Project(string filepath) : this()
        {
            Initialize(filepath);
        }
        public void Initialize(string filepath)
        {
            ProjFile = ReadProject(filepath);
            Populate();
        }
        public void Populate()
        {
            PopulateACMD();
            PopulateMSC();
            PopulateParams();
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
        public string MotionFolder { get; set; }
        public MTable MotionTable { get; set; }

        public Dictionary<uint, string> AnimationHashes { get { return _animHashes; } set { _animHashes = value; } }
        private Dictionary<uint, string> _animHashes;

        public ParamFile Attributes { get; set; }
        public ParamFile Fighter_Param_vl { get; set; }

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
            }
            else if (Filepath.EndsWith(".mscsb")) // MSC
            {
                MSC_FILES.Add(Path.GetFileNameWithoutExtension(Filepath), new MSCFile(Filepath));
                handled = true;
            }
            else if (Filepath.EndsWith(".mtable"))
            {
                MotionTable = new MTable(Filepath, Runtime.WorkingEndian);
            }

            return handled;
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

            node = proj.SelectSingleNode("//Project/ATTR");
            Attributes = new ParamFile(Path.GetDirectoryName(filepath) + $"/{node.Attributes["include"].Value}");

            node = proj.SelectSingleNode("//Project/PARAMS");
            Fighter_Param_vl = new ParamFile(Path.GetDirectoryName(filepath) + $"/{node.Attributes["include"].Value}");

            node = proj.SelectSingleNode("//Project/ANIM");
            MotionFolder = Path.GetDirectoryName(filepath) + "/ANIM";
            foreach (XmlNode child in node.ChildNodes)
            {
                ANIM_FILES.Add(child.Attributes["include"].Value);
                Runtime.ParseAnim($"{MotionFolder}/{child.Attributes["include"].Value.Substring(5)}", ref _animHashes);
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

            writer.WriteStartElement("ATTR");
            writer.WriteAttributeString("include", Path.Combine("PARAM", $"Fighter_Param.bin"));
            writer.WriteEndElement();

            writer.WriteStartElement("PARAMS");
            writer.WriteAttributeString("include", Path.Combine("PARAM", $"fighter_param_vl_{ProjName}.bin"));
            writer.WriteEndElement();

            writer.WriteStartElement("ACMD");
            foreach (var acmd in ACMD_FILES)
            {
                writer.WriteStartElement("Import");
                writer.WriteAttributeString("include", $"ACMD{Path.DirectorySeparatorChar + acmd.Key}.bin");
                writer.WriteEndElement();
            }
            writer.WriteStartElement("Import");
            writer.WriteAttributeString("include", $"ACMD{Path.DirectorySeparatorChar}Motion.mtable");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("MSC");
            foreach (var msc in MSC_FILES)
            {
                writer.WriteStartElement("Import");
                writer.WriteAttributeString("include", $"MSC{Path.DirectorySeparatorChar + msc.Key}.mscsb");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("ANIM");
            foreach (var anim in ANIM_FILES)
            {
                writer.WriteStartElement("Import");
                writer.WriteAttributeString("include",
                    $"ANIM{Path.DirectorySeparatorChar + anim.Substring(anim.IndexOf("ANIM") + 5)}");

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
        public void Save()
        {
            Save(ProjRoot);
        }
        public void Save(string rootpath)
        {
            var path = Path.Combine(rootpath, "ACMD");
            Directory.CreateDirectory(path);

            foreach (var keypair in ACMD_FILES)
                keypair.Value.Export(Path.Combine(path, $"{keypair.Key}.bin"));

            using (var stream = File.Create(Path.Combine(path, "motion.mtable")))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (uint u in MotionTable)
                    {
                        if (Runtime.WorkingEndian == Endianness.Big)
                            writer.Write(BitConverter.GetBytes(u.Reverse()));
                        else
                            writer.Write(u);
                    }
                }
            }
            path = Path.Combine(rootpath, "MSC");
            Directory.CreateDirectory(path);
            foreach (var keypair in MSC_FILES)
                keypair.Value.Export(Path.Combine(path, $"{keypair.Key}.mscsb"));

            path = Path.Combine(rootpath, "PARAM");
            Directory.CreateDirectory(path);
            Fighter_Param_vl.Export(Path.Combine(path, $"fighter_param_vl_{ProjName}.bin"));
            Attributes.Export(Path.Combine(path, "fighter_param.bin"));

            path = Path.Combine(rootpath, "ANIM");
            DirectoryX.Copy(MotionFolder, path);
            //foreach (string s in ANIM_FILES)
            //{
            //    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(path, s)));
            //    var tmp1 = Path.Combine(rootpath, s);
            //    var tmp2 = Path.Combine(path, s);
            //    File.Copy(tmp1, tmp2, true);
            //}
            ProjPath = Path.Combine(rootpath, $"{ProjName}.fitproj");
            ProjFile = WriteFitproj(ProjPath);
        }

        public void PopulateACMD()
        {
            ACMDNode.Nodes.Clear();
            foreach (uint u in MotionTable)
            {
                ScriptNode snode = new ScriptNode(u, $"{MotionTable.IndexOf(u)} - {u.ToString("X8")}");

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
                int index = file.Scripts.IndexOfValue(scr.Value);
                var snode = new ScriptNode((uint)index, $"Script_{index}");
                snode.Scripts.Add("Script", scr.Value);
                MSCNode.Nodes.Add(snode);
            }
        }
        public void PopulateParams()
        {
            int group = 0;
            foreach (var grp in Fighter_Param_vl.Groups)
            {
                var groupnode = new TreeNode($"Group[{group}]");
                if (grp is ParamGroup)
                {
                    int entry = 0;
                    foreach (var chunk in ((ParamGroup)grp).Chunks)
                    {
                        var node = new ParamListNode(group, entry) { Text = $"Entry[{entry}]" };
                        foreach (var val in chunk)
                            node.Parameters.Add(val);
                        groupnode.Nodes.Add(node);
                        entry++;
                    }
                }
                else
                {
                    var node = new ParamListNode(group, 0) { Text = $"Values[{group}]" };
                    foreach (var col in grp.Values)
                    {
                        node.Parameters.Add(new ParamEntry(col.Value, col.Type));
                    }
                    groupnode.Nodes.Add(node);
                }
                PARAMNode.Nodes.Add(groupnode);
                group++;
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
