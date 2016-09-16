//==================================================================\\
//                  RichTextBox ToolTip control                     \\
// Useful RichTextBox tooltip with advanced user interface settings \\
//                      ElHorri Soft                                \\
//                      RTB_ToolTip                                 \\
//                   Copyright ©  2012                              \\
//          RTB_ToolTip is a trademark of ElHorri Soft              \\
//==================================================================\\

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Sm4shCommand
{
    [EditorBrowsable(EditorBrowsableState.Always)]
    public class ITSToolTip
    {
        public ITSToolTip()
        {
            ttp = new eToolTip() { UseAnimation = true, UseFading = true };
            synChecking.RTBTT = this;
        }

        private TooltipDictionary _dict = new TooltipDictionary();

        /// <summary>
        /// Gets or sets a specified dictionary to use with the specified RichTextBox control.
        /// </summary>
        public TooltipDictionary Dictionary
        {
            get { return _dict; }
            set { synChecking.Dictionary = value; _dict = value; }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public eToolTip ttp;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SyntaxChecking synChecking = new SyntaxChecking();


        /// <summary>
        /// Gets or sets the pre-title text string that will be desplayed before the title text.
        /// </summary>
        [DefaultValue("")]
        public string TitlePrefix { get { return ttp.Prefix; } set { ttp.Prefix = value; } }

        /// <summary>
        /// Gets or sets the after-title text that will be desplayed after the title text.
        /// </summary>
        [DefaultValue("")]
        public string TitleSuffix { get { return ttp.Suffix; } set { ttp.Suffix = value; } }

        /// <summary>
        ///     Gets or sets a value determining whether an animation effect should be used
        ///     when displaying the ToolTip.
        ///
        ///     Returns true if window animation should be used; otherwise, false. The default is
        ///     true.
        /// </summary>
        public bool UseAnimation
        {
            get { return ttp.UseAnimation; }
            set { ttp.UseAnimation = value; }
        }

        /// <summary>
        ///     Gets or sets a value determining whether a fade effect should be used when
        ///     displaying the ToolTip.
        ///
        /// Returns true if window fading should be used; otherwise, false. The default is true.
        /// <summary>    
        public bool UseFading
        {
            get { return ttp.UseFading; }
            set { ttp.UseFading = value; }
        }

        private bool _isEnabled = true;
        /// <summary>
        /// Enables or desables the tooltip control.
        /// The control is enabled by default.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return this._isEnabled;
            }
            set
            {
                if (value)
                {
                    this._isEnabled = value;
                    RichTextBox.MouseMove += new MouseEventHandler(synChecking.referencedRTB_MouseMove);
                }
                else
                {
                    this._isEnabled = value;
                    RichTextBox.MouseMove -= new MouseEventHandler(synChecking.referencedRTB_MouseMove);
                };
            }
        }

        /// <summary>
        /// Gets or sets the tooltip title text
        /// </summary>
        public string Title
        {
            get { return ttp.ToolTipTitle; }
            set { ttp.ToolTipTitle = value; }
        }

        /// <summary>
        /// Gets or sets the tooltip description text
        /// </summary>
        public string Description
        {
            get { return ttp.ToolTipDescription; }
            set { ttp.ToolTipDescription = value; }
        }

        /// <summary>
        /// Sets or gets the title forecolor
        /// </summary>
        public Brush TitleBrush
        {
            get { return ttp.TitleBrush; }
            set
            {
                ttp.TitleBrush = value;
            }
        }

        /// <summary>
        /// Sets or gets the description forecolor
        /// </summary>
        public Brush DescriptionBrush
        {
            get { return ttp.DescriptionBrush; }
            set
            {
                ttp.DescriptionBrush = value;
            }
        }

        /// <summary>
        /// Sets or gets the background image of the tooltip
        /// </summary>
        public Bitmap BackgroundImage
        {
            get { return ttp.BackgroundImage; }
            set
            {
                ttp.BackgroundImage = value;
            }
        }

        /// <summary>
        /// Sets or gets the tooltip's title font
        /// </summary>
        public Font TitleFont
        {
            get { return ttp.TitleFont; }
            set
            {
                ttp.TitleFont = value;
            }
        }

        /// <summary>
        /// SSets or getspecifies the tooltip's description font
        /// </summary>
        public Font DescriptionFont
        {
            get { return ttp.DescriptionFont; }
            set
            {
                ttp.DescriptionFont = value;
            }
        }

        /// <summary>
        /// The control that will hold the tooltip
        /// </summary>
        public RichTextBox RichTextBox
        {
            get { return ttp.SelectedControl; }
            set
            {
                value.MouseMove += new MouseEventHandler(synChecking.referencedRTB_MouseMove);
                ttp.SelectedControl = value;
            }
        }
        /// <summary>
        /// Gets or sets the chars that the SyntaxChecking class relies on to determine words' boundaries.
        /// </summary>
        public List<Char> Chars
        {
            get { return synChecking.Chars; }
            set { synChecking.Chars = value; }
        }

        /// <summary>
        /// Shows the tooltip
        /// </summary>
        public void Show() { ttp.Show(); }

        /// <summary>
        /// Hides the toltip
        /// </summary>
        public void Hide() { ttp.Hide(); }
    }

    /// <summary>
    /// This will hold info items for our dictionary
    /// </summary>
    [Serializable()]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DictionaryInfo
    {
        private string _ttl;
        private string _dsc;
        private int _indx;
        public string Title
        {
            get { return _ttl; }
            set { _ttl = value; }
        }
        public string Description
        {
            get { return _dsc; }
            set { _dsc = value; }
        }
        public int Index
        {
            get { return _indx; }
            set { _indx = value; }
        }
    }

    /// <summary>
    /// Contains methods for manipulating the dictionary used for our RichTextBox
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public class TooltipDictionary
    {
        private List<DictionaryInfo> dicList = new List<DictionaryInfo>();

        /// <summary>
        /// Adds a new dictionary info item to the dictionary
        /// </summary>
        /// <param name="inf">The info item to add as a 'DictionaryInfo'</param>
        public void Add(DictionaryInfo inf)
        {
            inf.Index = dicList.Count + 1;
            dicList.Add(inf);
        }

        /// <summary>
        /// Adds a new dictionary info item to the dictionary
        /// </summary>
        /// <param name="inf">The info item to add as a 'DictionaryInfo'</param>
        public void Add(string title, string description)
        {
            var inf = new DictionaryInfo() { Title = title, Description = description };
            inf.Index = dicList.Count + 1;
            dicList.Add(inf);
        }

        /// <summary>
        /// Removes an existing items from the dictionary
        /// </summary>
        /// <param name="inf">The info item to remove as a 'DictionaryInfo'</param>
        public void Remove(DictionaryInfo inf)
        {
            try
            {
                dicList.Remove(inf);
            }
            catch { throw new Exception("InfNotFoundInDictinaryException"); };
        }

        /// <summary>
        /// Removes an info item at a specified index from the dictionary
        /// </summary>
        /// <param name="infIndex">The info-item's index to target</param>
        public void RemoveAt(int infIndex)
        {
            try
            {
                dicList.RemoveAt(infIndex);
            }
            catch { throw new Exception("IndexNotFoundException"); }
        }

        /// <summary>
        /// Checks wheather a dictionary info item that has the same 
        /// Title, Description and Index exists in the dictionary or not
        /// </summary>
        /// <param name="inf">The dictionary info item to check for</param>
        /// <returns></returns>
        public bool Exists(DictionaryInfo inf)
        {
            return dicList.Contains(inf);
        }

        /// <summary>
        /// Checks wheather the dictionary contains an info item that has the
        /// same Title as the specified one
        /// </summary>
        /// <param name="infTitle">The dictionary info item to check for</param>
        /// <returns></returns>
        public bool Contains(string infTitle)
        {
            bool _yes = false;
            foreach (var e in dicList) { if (e.Title == infTitle) _yes = true; };
            return _yes;
        }

        /// <summary>
        /// Returns all the occurences of the specified title whithin the dictionary
        /// </summary>
        /// <param name="infTitle">The dictionary info item to get</param>
        /// <returns>If no occurence was found a new empty list will be returned</returns>
        public List<DictionaryInfo> GetByTitle(string infTitle)
        {
            List<DictionaryInfo> _itms = new List<DictionaryInfo>();
            foreach (var e in dicList) { if (e.Title == infTitle) _itms.Add(e); };
            return _itms;
        }

        /// <summary>
        /// Checks wheather the dictionary contains an info item that has the
        /// same Index as the specified one
        /// </summary>
        /// <param name="infIndex">The dictionary info item to check for</param>
        /// <returns></returns>
        public bool Contains(int infIndex)
        {
            bool _yes = false;
            foreach (var e in dicList) { if (e.Index == infIndex) _yes = true; };
            return _yes;
        }

        /// <summary>
        /// Edits the specified dictionary info item according to the specified value
        /// </summary>
        /// <param name="inf">The desired info item from the dictionary</param>
        /// <param name="title">The new title. This value can be null to return the existing title</param>
        /// <param name="description">The new description. This value can be null to return the existing description</param>
        public void Edit(DictionaryInfo inf, string title, string description)
        {
            if (dicList.Contains(inf))
            {
                var x = new DictionaryInfo() { Index = inf.Index, Title = title, Description = description };
                dicList.RemoveAt(inf.Index);

                dicList.Insert(inf.Index, x);
            }
            else
            {
                throw new Exception("ElementNotFoundException");
            };
        }

        /// <summary>
        /// Returns a value indicating the capacity of this dictionary
        /// </summary>
        public int Capacity
        {
            get { return dicList.Capacity; }
            set { dicList.Capacity = value; }
        }

        /// <summary>
        /// Holds all values that were defined on the current class variable.
        /// </summary>
        public List<DictionaryInfo> Dictionary
        {
            get { return dicList; }
            set
            {
                foreach (var x in value)
                {
                    if (this.Contains(x.Index))
                    {
                        throw new Exception("An index of '" + x.Index + "' already exists in the collection!\nNone of the specified items were been added");
                    };
                };

                this.AddRange(value);
            }
        }

        public void AddRange(IEnumerable<DictionaryInfo> list)
        {
            foreach (var i in list)
            {
                var x = (DictionaryInfo)i;
                x.Index = dicList.Count + 1;
                dicList.Add(x);
            };
        }
    }

    public class SyntaxChecking
    {

        public TooltipDictionary Dictionary = new TooltipDictionary();

        /// <summary>
        /// An already set character-list that contains characters used as word boundaries.
        /// You can reset this list to your own one.
        /// </summary>
        public List<char> Chars = new List<char>() {'-','.',',',';',':','!','?','%','/','\\','+',
                                              '=','*','$','#','@','\'','\"','<','>','^','(',')',
                                              '[',']','{','}','°','&','|',' ' };

        private bool _isVisible = false;


        [EditorBrowsable(EditorBrowsableState.Never)]
        public ITSToolTip RTBTT { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void referencedRTB_MouseMove(object sender, MouseEventArgs e)
        {
            var cPosIndex = RTBTT.RichTextBox.GetCharIndexFromPosition(RTBTT.RichTextBox.PointToClient(Control.MousePosition));
            string hWord = GetWordFromCharIndex(cPosIndex);

            if (Dictionary.Contains(hWord))
            {
                var mp = Control.MousePosition;
                var x = Dictionary.GetByTitle(hWord)[0];
                RTBTT.Title = x.Title;
                RTBTT.Description = x.Description;
                if (!_isVisible)
                {
                    _isVisible = true;
                    RTBTT.Show();
                }
            }
            else
            {
                RTBTT.Hide();
                _isVisible = false;
            };
        }

        private struct CompoundedPos
        {
            public Point StartPos;
            public Point EndPos;
        };
        private string GetWordFromCharIndex(int cIndex)
        {
            string tmp = string.Empty;
            if (RTBTT.RichTextBox.TextLength > 0)
            {
                if (!IsMousePosOutOfLastLine() & WordIsTargetedByMouse(cIndex))
                {
                    // check wheather word is bounded between its start & end positions
                    var line = RTBTT.RichTextBox.GetLineFromCharIndex(cIndex);
                    var lineStr = string.Empty;
                    try
                    {
                        lineStr = RTBTT.RichTextBox.Lines[line];
                    }
                    catch { return tmp; };

                    int fCharIndex = RTBTT.RichTextBox.GetFirstCharIndexFromLine(line);
                    int eCharIndex;
                    //MessageBox.Show(line.ToString());
                    int selIndexWithinLine = cIndex - fCharIndex;

                    if (RTBTT.RichTextBox.Lines.Length == line) eCharIndex = RTBTT.RichTextBox.Text.Length;
                    else eCharIndex = RTBTT.RichTextBox.GetFirstCharIndexFromLine(line + 1) - 1;

                    int tmpCS = selIndexWithinLine;
                    int tmpCE = selIndexWithinLine;

                    try
                    {
                        if (tmpCS > 0)
                            while (!Chars.Contains(lineStr[tmpCS - 1]))
                            {
                                tmpCS--;
                            }
                    }
                    catch { };

                    int ln = lineStr.Length;

                    try
                    {
                        if (tmpCE > 0)
                            while (!Chars.Contains(lineStr[tmpCE]))
                            {
                                tmpCE++;
                            }
                    }
                    catch { };
                    try
                    {
                        tmp = lineStr.Substring(tmpCS, tmpCE - tmpCS);
                    }
                    catch { };
                }
            }
            return tmp.Trim();
        }
        private CompoundedPos GetLastLinePos()
        {
            Point p = new Point(0, 0);
            int ln = RTBTT.RichTextBox.Lines.Length - 1;
            var cp = RTBTT.RichTextBox.GetPositionFromCharIndex(RTBTT.RichTextBox.GetFirstCharIndexFromLine(ln));

            return new CompoundedPos()
            {
                StartPos = cp,
                EndPos = cp
                //new Point(cp.X + referencedRTB.Lines[ln - 1].Length * referencedRTB.Font.Height, cp.Y) 
            };
        }
        private bool IsMousePosOutOfLastLine()
        {
            var xp = GetLastLinePos();
            //MessageBox.Show(xp.StartPos.ToString());
            var vp = xp.StartPos.Y + RTBTT.RichTextBox.Font.Height;

            return RTBTT.RichTextBox.PointToClient(Control.MousePosition).Y > vp;
        }
        private CompoundedPos GetWordPosBoundaries(int charIndex)
        {
            Point x1 = new Point(0, 0);
            Point x2 = new Point(0, 0);
            int y = 0;

            var line = RTBTT.RichTextBox.GetLineFromCharIndex(charIndex);
            var lineStr = string.Empty;
            try
            {
                lineStr = RTBTT.RichTextBox.Lines[line];
            }
            catch { return new CompoundedPos(); };

            int fCharIndex = RTBTT.RichTextBox.GetFirstCharIndexFromLine(line); // line start
            int eCharIndex; // line end

            int selIndexWithinLine = charIndex - fCharIndex;

            if (RTBTT.RichTextBox.Lines.Length == line) eCharIndex = RTBTT.RichTextBox.Text.Length;
            else eCharIndex = RTBTT.RichTextBox.GetFirstCharIndexFromLine(line + 1) - 1;

            int tmpCS = selIndexWithinLine;
            int tmpCE = selIndexWithinLine;

            try
            {
                    while (!Chars.Contains(lineStr[tmpCS - 1]))
                    {
                        tmpCS--;
                    }
            }
            catch { };

            int ln = lineStr.Length;

            try
            {
                    while (!Chars.Contains(lineStr[tmpCE]))
                    {
                        tmpCE++;
                    }
            }
            catch { };
            try
            {
                //tmp = lineStr.Substring(tmpCS, tmpCE - tmpCS);
                y = RTBTT.RichTextBox.GetPositionFromCharIndex(fCharIndex).Y + RTBTT.RichTextBox.Font.Height;
                x1 = new Point(RTBTT.RichTextBox.GetPositionFromCharIndex(tmpCS + fCharIndex).X - 2, y);
                x2 = new Point(RTBTT.RichTextBox.GetPositionFromCharIndex(tmpCE + fCharIndex).X, y);
            }
            catch { };

            return new CompoundedPos() { StartPos = x1, EndPos = x2 };
        }
        private bool WordIsTargetedByMouse(int charIndex)
        {
            var wPos = GetWordPosBoundaries(charIndex);
            var mPos = RTBTT.RichTextBox.PointToClient(Control.MousePosition);

            int wSX = wPos.StartPos.X;
            int wEX = wPos.EndPos.X;
            int mX = mPos.X;
            return (mX > wSX & mX < wEX & mPos.Y <= wPos.StartPos.Y);
        }
    }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignTimeVisible(false)]
    public class eToolTip : ToolTip
    {
        #region Hidden methods & props

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public bool Active { get; set; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public int AutomaticDelay { get { return base.AutomaticDelay; } set { base.AutomaticDelay = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public int AutoPopDelay { get { return base.AutoPopDelay; } set { base.AutoPopDelay = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public Color BackColor { get { return base.BackColor; } set { base.BackColor = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override CreateParams CreateParams { get { return base.CreateParams; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public Color ForeColor { get { return base.ForeColor; } set { base.ForeColor = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public int InitialDelay { get { return base.InitialDelay; } set { base.InitialDelay = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public bool IsBalloon { get { return base.IsBalloon; } set { base.IsBalloon = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public bool OwnerDraw { get { return base.OwnerDraw; } set { base.OwnerDraw = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public int ReshowDelay { get { return base.ReshowDelay; } set { base.ReshowDelay = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public bool ShowAlways { get { return base.ShowAlways; } set { base.ShowAlways = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public bool StripAmpersands { get { return base.StripAmpersands; } set { base.StripAmpersands = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public object Tag { get { return base.Tag; } set { base.Tag = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public ToolTipIcon ToolTipIcon { get { return base.ToolTipIcon; } set { base.ToolTipIcon = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public string ToolTipTitle { get { return base.ToolTipTitle; } set { base.ToolTipTitle = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public bool UseAnimation { get { return base.UseAnimation; } set { base.UseAnimation = value; } }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new public bool UseFading { get { return base.UseFading; } set { base.UseFading = value; } }

        #endregion

        #region Done!
        public eToolTip()
        {
            this.OwnerDraw = true;
            this.Popup += new PopupEventHandler(this.OnPopup);
            this.Draw += new DrawToolTipEventHandler(this.OnDraw);
            this.InitialDelay = 200;
            this.ReshowDelay = 500;
            // Force the ToolTip text to be displayed whether or not the control active.
            this.ShowAlways = true;
        }

        /// <summary>
        /// Gets or sets a pre-title string.
        /// </summary>
        [DefaultValue("")]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets a after-title string.
        /// </summary>
        [DefaultValue("")]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Suffix { get; set; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Show()
        {
            this.SetToolTip(SelectedControl, "CAPTION_TEST");
        }
        private static RichTextBox _selCtrl = new RichTextBox();

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RichTextBox SelectedControl
        {
            get { return _selCtrl; }
            set { _selCtrl = value; }
        }
        private Size _definedSZ = new Size(100, 70);
        private bool _autoCalcSz = true;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Size Size
        {
            get { return _definedSZ; }
            set { _definedSZ = value; }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool AutoSize
        {
            get { return _autoCalcSz; }
            set { _autoCalcSz = value; }
        }
        private Bitmap ResizeBitmap(Bitmap srcImage, int newWidth, int newHeight)
        {
            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                // Drawing our custom bg
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
            }

            return newImage;
        }
        private static string _tdsc = string.Empty;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string ToolTipDescription
        {
            get { return _tdsc; }
            set
            {
                string _tmp = value;
                while (_tmp.Contains("  ")) { _tmp = _tmp.Replace("  ", " "); };
                while (_tmp.Contains("\n\n")) { _tmp = _tmp.Replace("\n\n", "\n"); };
                //MessageBox.Show(_tmp);
                _tdsc = _tmp;
            }
        }
        private static Bitmap _bg = new Bitmap(50, 50);
        private static Brush _eBrush = Brushes.LightBlue;
        private static Brush _eDsc = Brushes.LightGray;
        private const float _fSz = 10F;
        private static Font _dscF = new Font("Times new roman", _fSz, FontStyle.Regular);
        private static Font _uiF = new Font("Times new roman", _fSz, FontStyle.Bold);

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Brush TitleBrush
        {
            get { return _eBrush; }
            set
            {
                if (value == null)
                {
                    _eBrush = Brushes.Black;
                }
                else
                {
                    _eBrush = value;
                }
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Brush DescriptionBrush
        {
            get { return _eDsc; }
            set
            {
                if (value == null)
                {
                    _eDsc = Brushes.Black;
                }
                else
                {
                    _eDsc = value;
                }
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Bitmap BackgroundImage
        {
            get { return _bg; }
            set
            {
                if (value == null)
                {
                    return;
                }
                else
                {
                    _bg = value;
                }
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Font TitleFont
        {
            get { return _uiF; }
            set
            {
                _uiF = value;
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Font DescriptionFont
        {
            get { return _dscF; }
            set
            {
                _dscF = value;
            }
        }
        private int _maxW = 0;
        // Summary:
        //      This sets a maximum value for the width of the tooltip.
        //      For desabling MaxWidth reset this to a 0 value.
        //      If this value is smaller than the calculated width of the title text the width of the title will be used as a minimum width for the tooltip. 
        // Note: 
        //      by assigning a value to this please avoid using 'new line' characters, such as '\n' character.

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int MaxWidth
        {
            get { return _maxW; }
            set { _maxW = value; }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Hide()
        {
            this.SetToolTip(SelectedControl, "");
        }
        private Size tSize = new Size(0, 0);

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        private void OnPopup(object sender, PopupEventArgs e)
        {

            //MessageBox.Show(RichTextBoxToolTip.TitlePrefix);
            if (!AutoSize)
            {
                e.ToolTipSize = Size;
            }
            else
            {
                var r = new Bitmap(e.ToolTipSize.Width, e.ToolTipSize.Height);
                var g = Graphics.FromImage(r);
                r.Dispose();
                var szT = g.MeasureString(Prefix + this.ToolTipTitle + Suffix, this.TitleFont);
                var szD = g.MeasureString(this.ToolTipDescription, this.DescriptionFont);

                MinWidth = (int)szT.Width + 10;
                if (szT.Width > szD.Width)
                {
                    // Set tooltip's size according to the Title text property
                    if (String.IsNullOrEmpty(ToolTipDescription))
                    {
                        // Set tooltip size without respecting the description's size
                        e.ToolTipSize = new Size((int)szT.Width + 10, (int)(szT.Height) + 10);
                    }
                    else
                    {
                        // Set tooltip size by taking into account the description's size
                        var w = szT.Width;
                        if (w < szD.Width) { w = szD.Width; };
                        var h = szT.Height + szD.Height;
                        e.ToolTipSize = new Size((int)w + 10, (int)h + 20);
                    };
                }
                else
                {
                    if (MaxWidth != 0)
                    {
                        var ex = g.MeasureString(this.ToolTipDescription, this.DescriptionFont, MaxWidth);
                        int mVal = MaxWidth;
                        if (mVal == 0) mVal = 1;
                        int w = ((int)ex.Width) + 10;
                        int h = (int)(ex.Height);
                        int wrappedLines = w / mVal;
                        if (wrappedLines == 0) wrappedLines = 1;
                        w /= wrappedLines;
                        h *= wrappedLines;
                        if (w < MinWidth)
                        {
                            e.ToolTipSize = new Size(MinWidth, (int)(h + szT.Height) + (TitleFont.Height + 10));
                        }
                        else
                        {
                            e.ToolTipSize = new Size(w + 10, h + (int)szT.Height + (TitleFont.Height + 10));
                        };
                    }
                    else
                    {
                        int w = ((int)szD.Width) + 10;
                        int h = (int)(szD.Height);
                        tSize = new Size(w + 10, h + (int)szT.Height);
                        tSize.Height += (TitleFont.Height + 10);
                        e.ToolTipSize = tSize;
                    };
                };

            };
        }
        private int MinWidth;
        private void OnDraw(object sender, DrawToolTipEventArgs e) // use this event to customise the tool tip
        {
            // Set our background
            Graphics g = e.Graphics;
            var n = BackgroundImage.Clone() as Bitmap;
            var rectSz = e.Bounds.Size;
            n = ResizeBitmap(n, rectSz.Width, rectSz.Height);
            g.Clear(Color.FromArgb(50, 50, 50));
            g.DrawImage(n, 0, 0);
            e.DrawBorder();

            // Draw our ToolTip title
            var rectF = new RectangleF(new PointF(8, TitleFont.Height + 10), new Size(tSize.Width - 8, tSize.Height));
            if (MaxWidth != 0)
            {
                g.DrawString(ToolTipDescription, DescriptionFont, DescriptionBrush, rectF);
            }
            else
            {
                g.DrawString(ToolTipDescription, DescriptionFont, DescriptionBrush, rectF.Location);
            };
            g.DrawString((Prefix + this.ToolTipTitle + Suffix).Trim('\n'), TitleFont, TitleBrush, new PointF(6, 4));

        }
        #endregion
    }
}

