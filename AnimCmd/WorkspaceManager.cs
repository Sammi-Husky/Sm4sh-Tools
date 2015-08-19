using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sm4shCommand
{
    class WorkspaceManager
    {
        private List<ProjectTag> ProjTags = new List<ProjectTag>(1);

        public void ReadWRKSPC(string path)
        {
            using (StreamReader stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    string raw = stream.ReadLine();
                    if (raw.StartsWith("Project"))
                        ProjTags.Add(new ProjectTag(stream));
                }
            }
        }
        public void ReadProject(string path)
        {

        }
    }

    class BaseTag
    {
        public string Name { get { return _name; } set { _name = value; } }
        private string _name;

        public int NestingLevel { get { return _nestLevel; } }
        private int _nestLevel;

        public virtual void DeSerialize(StreamReader stream) { }
    }
    class PropTag : BaseTag
    {
        public PropTag(StreamReader stream) { }
    }
    class ProjectTag : BaseTag
    {
        public ProjectTag(StreamReader stream) { DeSerialize(stream); }

        public string ProjectName { get { return _projName; } set { _projName = value; } }
        private string _projName;

        public string FitProjPath { get { return _fitProj; } set { _fitProj = value; } }
        private string _fitProj;

        public List<PropTag> props = new List<PropTag>();

        public override void DeSerialize(StreamReader stream)
        {
            string raw = stream.ReadLine();

            Name = "Project";
            _projName = raw.Substring(8, raw.Length - raw.IndexOf(','));
            raw.Substring(raw.IndexOf(',') + 1).TrimEnd(new char[] { ';', ')' });

            while ((raw = stream.ReadLine()) != "endProj;")
                props.Add(new PropTag(stream));

        }
    }
}
