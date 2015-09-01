using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sm4shCommand
{
    class WorkspaceManager
    {
        public WorkspaceManager()
        {
            _projects = new List<Project>();
            WorkspaceRoot = "";
        }
        public List<Project> _projects;
        public string WorkspaceRoot;

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
        public void AddProj() { }
        public void RemoveProj() { }
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

        private List<string> _includedDirs = new List<string>();
        private List<string> _includedFiles = new List<string>();

        public string ProjectName { get { return _projectName; } set { _projectName = value; } }
        private string _projectName;

        public string ProjectRoot { get { return _root; } set { _root = value; } }
        private string _root;

        public string ProjectFile { get { return _projFile; } set { _projFile = value; } }
        private string _projFile;

        public ProjType ProjectType { get { return _type; } set { _type = value; } }
        private ProjType _type;


        public void AddDirectory(string dirPath)
        {
            if (!_includedDirs.Contains(dirPath))
                _includedDirs.Add(dirPath);
            else
                _includedDirs[_includedDirs.IndexOf(dirPath)] = dirPath;
        }
        public void RemoveDirectory(string DirPath) { }
        public void AddFile(string filePath)
        {
            if (!_includedFiles.Contains(filePath))
                _includedFiles.Add(filePath);
            else
                _includedFiles[_includedFiles.IndexOf(filePath)] = filePath;
        }
        public void RemoveFile(string filePath) { }
        private void Initialize(string path)
        {
            using (StreamReader stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    string raw = stream.ReadLine().Trim();
                    if (raw.StartsWith("Dir="))
                    {
                        AddDirectory(raw.Substring(4).Trim('\"'));
                        while ((raw = stream.ReadLine()) != "endDir")
                        {
                            if (raw.Trim().StartsWith("include="))
                                AddFile(raw.Substring(9).Trim('\"'));
                        }
                    }
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
