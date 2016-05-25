using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SALT.Scripting.AnimCMD;

namespace Sm4shCommand.Classes
{
    /// <summary>
    /// Holder class that represents all data associated with Fighters in SSB4
    /// </summary>
    public class Fighter
    {
        public Fighter() { _hashPairs = new Dictionary<uint, string>(); }
        /// <summary>
        /// Linked list containing all animation names and their CRC32 hash.
        /// </summary>
        public Dictionary<uint, string> AnimationHashPairs { get { return _hashPairs; } set { _hashPairs = value; } }
        private Dictionary<uint, string> _hashPairs;
        /// <summary>
        /// Main ACMD file.
        /// </summary>
        public ACMDFile Main { get { return _main; } set { _main = value; } }
        private ACMDFile _main;
        /// <summary>
        /// GFX ACMD file.
        /// </summary>
        public ACMDFile GFX { get { return _gfx; } set { _gfx = value; } }
        private ACMDFile _gfx;
        /// <summary>
        /// SFX ACMD file.
        /// </summary>
        public ACMDFile SFX { get { return _sfx; } set { _sfx = value; } }
        private ACMDFile _sfx;
        /// <summary>
        /// Expression ACMD file.
        /// </summary>
        public ACMDFile Expression { get { return _expression; } set { _expression = value; } }
        private ACMDFile _expression;
        /// <summary>
        /// Animation CRC table (.mtable)
        /// </summary>
        public MTable MotionTable { get { return _mtable; } set { _mtable = value; } }
        private MTable _mtable;

        public bool Dirty
        {
            get
            {
                for (int i = 0; i < 4; i++)
                    if (this[i].Dirty)
                        return true;
                return false;
            }
        }
        /// <summary>
        /// Dumps a fighters script in it's entirety as text for use in version diffing.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();

            foreach (uint u in MotionTable)
            {
                string label = "";
                AnimationHashPairs.TryGetValue(u, out label);
                if (string.IsNullOrEmpty(label))
                    label = $"{u:X8}";

                sb.Append(String.Format($"\n\n{MotionTable.IndexOf(u):X}: [{label}]"));
                ACMDScript c1 = null, c2 = null,
                            c3 = null, c4 = null;

                if (Main.Scripts.ContainsKey(u))
                    c1 = ((ACMDScript)Main.Scripts[u]);
                if (GFX.Scripts.ContainsKey(u))
                    c2 = ((ACMDScript)GFX.Scripts[u]);
                if (SFX.Scripts.ContainsKey(u))
                    c3 = ((ACMDScript)SFX.Scripts[u]);
                if (Expression.Scripts.ContainsKey(u))
                    c4 = ((ACMDScript)Expression.Scripts[u]);

                sb.Append("\n\tGame:{");
                if (c1 != null)
                    foreach (ACMDCommand cmd in c1)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tGFX:{");
                if (c2 != null)
                    foreach (ACMDCommand cmd in c2)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tSFX:{");
                if (c3 != null)
                    foreach (ACMDCommand cmd in c3)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tExpression:{");
                if (c4 != null)
                    foreach (ACMDCommand cmd in c4)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");
            }
            return sb.ToString();
        }
        public void Export(string dirpath)
        {
            Main.Export($"{dirpath}/game.bin");
            SFX.Export($"{dirpath}/sound.bin");
            GFX.Export($"{dirpath}/effect.bin");
            Expression.Export($"{dirpath}/expression.bin");
            MotionTable.Export($"{dirpath}/motion.mtable");
        }
        public ACMDFile this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Main;
                    case 1:
                        return GFX;
                    case 2:
                        return SFX;
                    case 3:
                        return Expression;
                    default:
                        return null;
                }

            }
            set
            {
                switch (index)
                {
                    case 0:
                        Main = value;
                        break;
                    case 1:
                        GFX = value;
                        break;
                    case 2:
                        SFX = value;
                        break;
                    case 3:
                        Expression = value;
                        break;
                }
            }
        }
    }
}
