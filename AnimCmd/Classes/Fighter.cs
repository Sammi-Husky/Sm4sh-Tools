using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sm4shCommand.Classes
{
    /// <summary>
    /// Holder class that represents all data associated with Fighters in SSB4
    /// </summary>
    public class Fighter
    {
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

        /// <summary>
        /// Dumps a fighters script in it's entirety as text for use in version diffing.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (uint u in MotionTable)
            {
                sb.Append(String.Format("\n\n{0:X}: [{1:X8}]", MotionTable.IndexOf(u), u));
                CommandList c1 = null, c2 = null,
                            c3 = null, c4 = null;

                if (Main.EventLists.ContainsKey(u))
                    c1 = Main.EventLists[u];
                if (GFX.EventLists.ContainsKey(u))
                    c2 = GFX.EventLists[u];
                if (SFX.EventLists.ContainsKey(u))
                    c3 = SFX.EventLists[u];
                if (Expression.EventLists.ContainsKey(u))
                    c4 = Expression.EventLists[u];

                sb.Append("\n\tGame:{");
                if (c1 != null)
                    foreach (Command cmd in c1.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tGFX:{");
                if (c2 != null)
                    foreach (Command cmd in c2.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tSFX:{");
                if (c3 != null)
                    foreach (Command cmd in c3.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tExpression:{");
                if (c4 != null)
                    foreach (Command cmd in c4.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");
            }
            return sb.ToString();
        }
        public ACMDFile this[int i]
        {
            get
            {
                switch (i)
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
        }
    }
}
