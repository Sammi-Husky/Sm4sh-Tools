using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using System.Windows.Forms;
using Sm4shCommand.Classes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Sm4shCommand
{
    unsafe class WorkspaceManager
    {
        public List<Project> _projects = new List<Project>();
        public string WorkspaceRoot = "";

        public void ReadWRKSPC(string path)
        {
            WorkspaceRoot = Path.GetDirectoryName(path);
            using (StreamReader stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    string raw = stream.ReadLine();
                    if (raw.StartsWith("Project"))
                    {

                        string _projName = raw.Substring(8, raw.IndexOf(',') - 8);
                        string _fitProj = raw.Substring(raw.IndexOf(',') + 1).TrimEnd(new char[] { ':', ')' });
                        Project _proj = new Project(_projName, String.Format("{0}/{1}", WorkspaceRoot, _fitProj.Trim()));

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
        public void WriteWRKSPC(string path) { }

        public Dictionary<uint, string> getAnimNames(string path)
        {
            byte[] filebytes = File.ReadAllBytes(path);
            int count = (int)Util.GetWord(filebytes, 8, Runtime.WorkingEndian);
            Dictionary<uint, string> hashpairs = new Dictionary<uint, string>();
            for (int i = 0; i < count; i++)
            {

                uint off = (uint)Util.GetWord(filebytes, 0x10 + (i * 4), Runtime.WorkingEndian);
                string FileName = Util.GetString(filebytes, off, Runtime.WorkingEndian);
                string AnimName = Regex.Match(FileName, @"(.*)([A-Z])([0-9][0-9])(.*)\.omo").Groups[4].ToString();

                hashpairs.Add(Crc32.Compute(Encoding.ASCII.GetBytes(AnimName.ToLower())), AnimName);

                if (AnimName.StartsWith("SpecialN") || AnimName.StartsWith("SpecialS") ||
                    AnimName.StartsWith("SpecialLw") || AnimName.StartsWith("SpecialHi") ||
                    AnimName.StartsWith("SpecialAirN") || AnimName.StartsWith("SpecialAirS") ||
                    AnimName.StartsWith("SpecialAirLw") || AnimName.StartsWith("SpecialAirHi"))
                {
                    hashpairs.Add(Crc32.Compute(Encoding.ASCII.GetBytes((AnimName + "_C2").ToLower())), AnimName + "_C2");
                    hashpairs.Add(Crc32.Compute(Encoding.ASCII.GetBytes((AnimName + "_C3").ToLower())), AnimName + "_C3");
                }

            }
            return hashpairs;
        }

        public ACMDFile OpenFile(string Filepath)
        {
            DataSource source = new DataSource(FileMap.FromFile(Filepath));

            if (*(byte*)(source.Address + 0x04) == 0x02)
                Runtime.WorkingEndian = Endianness.Little;
            else if ((*(byte*)(source.Address + 0x04) == 0x00))
                Runtime.WorkingEndian = Endianness.Big;
            else
            {
                return null;
            }

            return new ACMDFile(source, Runtime.WorkingEndian);
        }
        public Fighter OpenFighter(string dirPath)
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
            catch (FileNotFoundException x) { return null; }

            Runtime.isRoot = true;
            Runtime.rootPath = dirPath;
            Runtime.Instance.Text = String.Format("Main Form - {0}", dirPath);
            return f;
        }
        public MTable ParseMTable(DataSource source, Endianness endian)
        {
            List<uint> CRCTable = new List<uint>();

            for (int i = 0; i < source.Length; i += 4)
                //if((uint)Util.GetWordUnsafe((source.Address + i), endian) != 0)
                CRCTable.Add((uint)Util.GetWordUnsafe((source.Address + i), endian));

            return new MTable(CRCTable, endian);
        }
    }

    class Project
    {
        public Project(string Name, string projPath)
        {
            _projectName = Name;
            _projFile = projPath;
            _root = Path.GetDirectoryName(projPath);
            Initialize(projPath);
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
                        _scriptRoot = _root + raw.Substring(6).Trim('\"');
                        break;
                    case "ACMD":
                        _acmdPath = _scriptRoot + raw.Substring(6).Trim('\"');
                        break;
                    case "MSCSB":
                        _mscsbPath = _scriptRoot + raw.Substring(7).Trim('\"');
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
                        _paramPath = _root + raw.Substring(7).Trim('\"');
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
                        _animRoot = _root + raw.Substring(6).Trim('\"');
                        break;
                    case "FILE":
                        _animFile = _root + raw.Substring(6).Trim('\"');
                        break;
                    case "EXTRACTED":
                        _extracted = raw.Substring(10).Trim('\"').Equals("true", StringComparison.InvariantCultureIgnoreCase);
                        break;
                }
            }
        }

    }
    public enum ProjType
    {
        Fighter,
        Weapon
    }
}
