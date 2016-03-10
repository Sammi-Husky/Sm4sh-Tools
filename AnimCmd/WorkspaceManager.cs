using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Sm4shCommand
{
    unsafe class WorkspaceManager
    {
        public List<Project> Projects { get { return _projects; } set { _projects = value; } }
        private List<Project> _projects;

        public string WorkspaceRoot { get { return _workspaceRoot; } set { _workspaceRoot = value; } }
        private string _workspaceRoot;

        public Dictionary<uint, string> AnimHashPairs = new Dictionary<uint, string>();


        public void ReadWRKSPC(string path)
        {
            _projects = new List<Project>();
            _workspaceRoot = Path.GetDirectoryName(path);
            using (StreamReader stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    string raw = stream.ReadLine();
                    if (raw.StartsWith("Project"))
                    {
                        string _projName = raw.Substring(8, raw.IndexOf(',') - 8);
                        string _fitProj = raw.Substring(raw.IndexOf(',') + 1).TrimEnd(new char[] { ':', ')' });
                        Project _proj = new Project(_projName, _fitProj.Trim());

                        Dictionary<string, string> props = new Dictionary<string, string>();
                        while ((raw = stream.ReadLine()) != "endproj;")
                            props.Add(raw.Substring(0, raw.IndexOf('=')), raw.Substring(raw.IndexOf('=') + 1).TrimEnd(new char[] { ';', '\"' }));

                        if (props.ContainsKey("type"))
                            switch (props["type"])
                            {
                                case "weapon":
                                    _proj.ProjectType = ProjType.Weapon;
                                    break;
                                case "fighter":
                                    _proj.ProjectType = ProjType.Fighter;
                                    break;
                            }

                        _projects.Add(_proj);
                    }
                }
            }
        }
        public void WriteWRKSPC(string path)
        {
            using (StreamWriter stream = new StreamWriter(path))
            {
                foreach (Project p in _projects)
                {
                    stream.WriteLine(String.Format("Project({0},{1}):", p.ProjectName, p.ProjectFile));
                    stream.WriteLine(string.Format("\ttype= {0};", p.ProjectType));
                    stream.WriteLine("endproj;");
                }
            }
        }

        public void NewWorkspace(string Name, string src, string dest)
        {
            string root = dest + Path.DirectorySeparatorChar + Name;
            string projroot = root + Path.DirectorySeparatorChar + Name;

            Directory.CreateDirectory(root);
            Directory.CreateDirectory(projroot);
            _projects.Add(ImportProject(Name, src, projroot));
            WriteWRKSPC(root + Path.DirectorySeparatorChar + Name + ".wrkspc");
        }
        public Project ImportProject(string name, string src, string dest)
        {
            Project p = new Project(name, null);
            string outfile = string.Format("{0}{1}{2}.fitproj", dest, Path.DirectorySeparatorChar, name);
            p.ProjectFile = outfile;
            Directory.CreateDirectory(dest + "/Script/AnimCmd");
            var files = Directory.EnumerateFiles(src + "/Script/AnimCmd/Body");
            foreach (string s in files)
                File.Copy(s, dest + "/Script/AnimCmd/" + Path.GetFileName(s));

            Directory.CreateDirectory(dest + "/Anim");
            File.Copy(src + "/Motion/Body/Main.pac", dest + "/Anim/main.pac");

            Directory.CreateDirectory(dest + "/Param");
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Parameter file.";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.Copy(dlg.FileName, dest + "/Param/" + dlg.SafeFileName);
                p.ParamPath = dest + "/Param/" + dlg.SafeFileName;
            }
            p.ACMDPath = dest + "/Script/AnimCmd";
            p.ScriptRoot = dest + "/Script";
            p.AnimationDirectory = dest + "/Anim";
            p.AnimationFile = dest + "/Anim/main.pac";
            p.ExtractedAnimations = false;
            p.ProjectType = ProjType.Fighter;
            p.WriteFITPROJ(outfile);
            return p;
        }

        public void GetAnimHashPairs(string path)
        {
            Dictionary<uint, string> hashpairs = new Dictionary<uint, string>();
            foreach (string s in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                if (s.EndsWith(".pac"))
                {
                    byte[] filebytes = File.ReadAllBytes(s);
                    int count = (int)Util.GetWord(filebytes, 8, Runtime.WorkingEndian);

                    for (int i = 0; i < count; i++)
                    {
                        uint off = (uint)Util.GetWord(filebytes, 0x10 + (i * 4), Runtime.WorkingEndian);
                        string FileName = Util.GetString(filebytes, off, Runtime.WorkingEndian);
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
                else if (s.EndsWith(".bch"))
                {
                    DataSource src = new DataSource(FileMap.FromFile(s));
                    int off = *(int*)(src.Address + 0x0C);
                    VoidPtr addr = src.Address + off;
                    while (*(byte*)addr != 0)
                    {
                        string AnimName = Regex.Match(s, @"(.*)([A-Z])([0-9][0-9])(.*)").Groups[4].ToString();
                        if (string.IsNullOrEmpty(AnimName))
                        {
                            addr += s.Length + 1;
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

                        addr += s.Length + 1;
                    }
                }
            }
        }
        private void AddAnimHash(string name)
        {
            if (AnimHashPairs.ContainsValue(name))
                return;

            AnimHashPairs.Add(Crc32.Compute(Encoding.ASCII.GetBytes(name.ToLower())), name);
        }
        public ACMDFile OpenFile(string Filepath)
        {
            return OpenFile(Filepath, ACMDType.NONE);
        }
        public ACMDFile OpenFile(string Filepath, ACMDType type)
        {
            DataSource source = new DataSource(FileMap.FromFile(Filepath));
            if (*(buint*)source.Address != 0x41434D44) // ACMD
            {
                MessageBox.Show("Not an ACMD file:\n" + Filepath);
                return null;
            }

            if (*(byte*)(source.Address + 0x04) == 0x02)
                Runtime.WorkingEndian = Endianness.Little;
            else if ((*(byte*)(source.Address + 0x04) == 0x00))
                Runtime.WorkingEndian = Endianness.Big;
            else
                return null;


            return new ACMDFile(source) { Type = type };
        }

        public Fighter OpenFighter(string dirPath)
        {
            return new Fighter()
            {

                Main = OpenFile(dirPath + "/game.bin", ACMDType.Main),
                GFX = OpenFile(dirPath + "/effect.bin", ACMDType.GFX),
                SFX = OpenFile(dirPath + "/sound.bin", ACMDType.SFX),
                Expression = OpenFile(dirPath + "/expression.bin", ACMDType.Expression),

                MotionTable = ParseMTable(new DataSource(FileMap.FromFile(dirPath + "/motion.mtable")), Runtime.WorkingEndian)
            };
        }
        public MTable ParseMTable(DataSource source, Endianness endian)
        {
            List<uint> CRCTable = new List<uint>();

            for (int i = 0; i < source.Length; i += 4)
                CRCTable.Add((uint)Util.GetWordUnsafe((source.Address + i), endian));

            return new MTable(CRCTable, endian);
        }
    }

    class Project
    {
        public Project(string Name, string projPath)
        {
            _projectName = Name;
            if (projPath != null)
            {
                _projFile = projPath;
                _root = Path.GetDirectoryName(projPath);
                Initialize(projPath);
            }
        }

        public string ProjectName { get { return _projectName; } set { _projectName = value; } }
        private string _projectName;

        public string ProjectRoot { get { return _root; } set { _root = value; } }
        private string _root;

        public string ProjectFile { get { return _projFile; } set { _projFile = value; } }
        private string _projFile;

        public ProjType ProjectType { get { return _type; } set { _type = value; } }
        private ProjType _type;

        public string ScriptRoot { get { return _scriptRoot; } set { _scriptRoot = value; } }
        private string _scriptRoot;

        public string ACMDPath { get { return _acmdPath; } set { _acmdPath = value; } }
        private string _acmdPath;

        public string MSCSBPath { get { return _mscsbPath; } set { _mscsbPath = value; } }
        private string _mscsbPath;

        public string ParamPath { get { return _paramPath; } set { _paramPath = value; } }
        private string _paramPath;

        public string AnimationDirectory { get { return _animRoot; } set { _animRoot = value; } }
        private string _animRoot;

        public string AnimationFile { get { return _animFile; } set { _animFile = value; } }
        private string _animFile;

        public bool ExtractedAnimations { get { return _extracted; } set { _extracted = value; } }
        private bool _extracted;

        private void Initialize(string path)
        {
            using (StreamReader stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    string raw = stream.ReadLine().Trim();
                    if (raw.StartsWith("Project"))
                        ProjectTagInit(stream);
                    else if (raw.StartsWith("Script"))
                        ScriptTagInit(stream);
                    else if (raw.StartsWith("Animation"))
                        AnimationTagInit(stream);
                }
            }
        }
        private void ScriptTagInit(StreamReader stream)
        {
            string raw;
            while ((raw = stream.ReadLine().Trim()) != "}")
            {
                if (raw == "{")
                    continue;

                switch (raw.Substring(0, raw.IndexOf('=')))
                {
                    case "ROOT":
                        _scriptRoot = raw.Substring(6).Trim('\"');
                        break;
                    case "ACMD":
                        _acmdPath = raw.Substring(6).Trim('\"');
                        break;
                    case "MSCSB":
                        _mscsbPath = raw.Substring(7).Trim('\"');
                        break;
                }
            }
        }
        private void ProjectTagInit(StreamReader stream)
        {
            string raw;
            while ((raw = stream.ReadLine().Trim()) != "}")
            {
                if (raw == "{")
                    continue;

                switch (raw.Substring(0, raw.IndexOf('=')))
                {
                    case "TYPE":
                        if (raw.Substring(6).Trim('\"').Equals("fighter", StringComparison.InvariantCultureIgnoreCase))
                            _type = ProjType.Fighter;
                        else if (raw.Substring(6).Trim('\"').Equals("weapon", StringComparison.InvariantCultureIgnoreCase))
                            _type = ProjType.Weapon;
                        break;
                    case "PARAM":
                        _paramPath = raw.Substring(7).Trim('\"');
                        break;
                }
            }
        }
        private void AnimationTagInit(StreamReader stream)
        {
            string raw;
            while ((raw = stream.ReadLine().Trim()) != "}")
            {
                if (raw == "{")
                    continue;

                switch (raw.Substring(0, raw.IndexOf('=')))
                {
                    case "ROOT":
                        _animRoot = raw.Substring(6).Trim('\"');
                        break;
                    case "FILE":
                        _animFile = raw.Substring(6).Trim('\"');
                        break;
                    case "EXTRACTED":
                        _extracted = raw.Substring(10).Trim('\"').Equals("true", StringComparison.InvariantCultureIgnoreCase);
                        break;
                }
            }
        }

        public void WriteFITPROJ() { WriteFITPROJ(_projFile); }
        public void WriteFITPROJ(string path)
        {
            using (StreamWriter stream = new StreamWriter(path))
            {
                stream.WriteLine("Project\n{");
                stream.WriteLine("\tTYPE= \"" + (_type == ProjType.Fighter ? "fighter" : "weapon") + "\"");
                stream.WriteLine("\tPARAM= \"" + _paramPath + "\"\n}\n");

                stream.WriteLine("Script\n{");
                stream.WriteLine("\tROOT= \"" + _scriptRoot + "\"");
                stream.WriteLine("\tACMD= \"" + _acmdPath + "\"");
                stream.WriteLine("\tMSCSB= \"" + _mscsbPath + "\"\n}\n");

                stream.WriteLine("Animation\n{");
                stream.WriteLine("\tROOT= \"" + _animRoot + "\"");
                stream.WriteLine("\tFILE= \"" + _animFile + "\"");
                stream.WriteLine("\tEXTRACTED= " + (_extracted ? "true" : "false") + "\n}");
            }
        }
    }
    public enum ProjType
    {
        Fighter,
        Weapon
    }
}
