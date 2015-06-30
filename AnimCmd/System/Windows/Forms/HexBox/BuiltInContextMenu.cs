using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Be.Windows.Forms {
    /// <summary>
    ///   Defines a build-in ContextMenuStrip manager for HexBox control to show Copy, Cut, Paste menu in contextmenu of the control.
    /// </summary>
    [TypeConverterAttribute(typeof (ExpandableObjectConverter))]
    public sealed class BuiltInContextMenu : Component {
        /// <summary>
        ///   Contains the HexBox control.
        /// </summary>
        private readonly HexBox _hexBox;

        /// <summary>
        ///   Contains the ContextMenuStrip control.
        /// </summary>
        private ContextMenuStrip _contextMenuStrip;

        /// <summary>
        ///   Contains the "Cut"-ToolStripMenuItem object.
        /// </summary>
        private ToolStripMenuItem _cutToolStripMenuItem;

        /// <summary>
        ///   Contains the "Copy"-ToolStripMenuItem object.
        /// </summary>
        private ToolStripMenuItem _copyToolStripMenuItem;

        /// <summary>
        ///   Contains the "Paste"-ToolStripMenuItem object.
        /// </summary>
        private ToolStripMenuItem _pasteToolStripMenuItem;

        /// <summary>
        ///   Contains the "Select All"-ToolStripMenuItem object.
        /// </summary>
        private ToolStripMenuItem _selectAllToolStripMenuItem;

        /// <summary>
        ///   Initializes a new instance of BuildInContextMenu class.
        /// </summary>
        /// <param name = "hexBox">the HexBox control</param>
        internal BuiltInContextMenu(HexBox hexBox) {
            _hexBox = hexBox;
            _hexBox.ByteProviderChanged += HexBoxByteProviderChanged;
        }

        /// <summary>
        ///   If ByteProvider
        /// </summary>
        /// <param name = "sender">the sender object</param>
        /// <param name = "e">the event data</param>
        private void HexBoxByteProviderChanged(object sender, EventArgs e) {
            CheckBuiltInContextMenu();
        }

        /// <summary>
        ///   Assigns the ContextMenuStrip control to the HexBox control.
        /// </summary>
        private void CheckBuiltInContextMenu() {
            if (DesignMode) return;
            if (_contextMenuStrip == null) {
                var cms = new ContextMenuStrip();
                _cutToolStripMenuItem = new ToolStripMenuItem(CutMenuItemTextInternal, CutMenuItemImage,
                                                              new EventHandler(CutMenuItemClick));
                cms.Items.Add(_cutToolStripMenuItem);
                _copyToolStripMenuItem = new ToolStripMenuItem(CopyMenuItemTextInternal, CopyMenuItemImage,
                                                               new EventHandler(CopyMenuItemClick));
                cms.Items.Add(_copyToolStripMenuItem);
                _pasteToolStripMenuItem = new ToolStripMenuItem(PasteMenuItemTextInternal, PasteMenuItemImage,
                                                                new EventHandler(PasteMenuItemClick));
                cms.Items.Add(_pasteToolStripMenuItem);
                cms.Items.Add(new ToolStripSeparator());
                _selectAllToolStripMenuItem = new ToolStripMenuItem(SelectAllMenuItemTextInternal,
                                                                    SelectAllMenuItemImage,
                                                                    new EventHandler(SelectAllMenuItemClick));
                cms.Items.Add(_selectAllToolStripMenuItem);
                cms.Opening += BuildInContextMenuStripOpening;
                _contextMenuStrip = cms;
            }
            if (_hexBox.ByteProvider == null && _hexBox.ContextMenuStrip != null)
                _hexBox.ContextMenuStrip = null;
            else if (_hexBox.ByteProvider != null && _hexBox.ContextMenuStrip == null)
                _hexBox.ContextMenuStrip = _contextMenuStrip;
        }

        /// <summary>
        ///   Before opening the ContextMenuStrip, we manage the availability of the items.
        /// </summary>
        /// <param name = "sender">the sender object</param>
        /// <param name = "e">the event data</param>
        private void BuildInContextMenuStripOpening(object sender, CancelEventArgs e) {
            _cutToolStripMenuItem.Enabled = _hexBox.CanCut();
            _copyToolStripMenuItem.Enabled = _hexBox.CanCopy();
            _pasteToolStripMenuItem.Enabled = _hexBox.CanPaste();
            _selectAllToolStripMenuItem.Enabled = _hexBox.CanSelect();
        }

        /// <summary>
        ///   The handler for the "Cut"-Click event
        /// </summary>
        /// <param name = "sender">the sender object</param>
        /// <param name = "e">the event data</param>
        private void CutMenuItemClick(object sender, EventArgs e) {
            _hexBox.Copy();
        }

        /// <summary>
        ///   The handler for the "Copy"-Click event
        /// </summary>
        /// <param name = "sender">the sender object</param>
        /// <param name = "e">the event data</param>
        private void CopyMenuItemClick(object sender, EventArgs e) {
            _hexBox.Copy();
        }

        /// <summary>
        ///   The handler for the "Paste"-Click event
        /// </summary>
        /// <param name = "sender">the sender object</param>
        /// <param name = "e">the event data</param>
        private void PasteMenuItemClick(object sender, EventArgs e) {
            _hexBox.Copy();
        }

        /// <summary>
        ///   The handler for the "Select All"-Click event
        /// </summary>
        /// <param name = "sender">the sender object</param>
        /// <param name = "e">the event data</param>
        private void SelectAllMenuItemClick(object sender, EventArgs e) {
            _hexBox.SelectAll();
        }

        /// <summary>
        ///   Gets or sets the custom text of the "Copy" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null), Localizable(true)]
        public string CopyMenuItemText { get; set; }

        /// <summary>
        ///   Gets or sets the custom text of the "Cut" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null), Localizable(true)]
        public string CutMenuItemText { get; set; }

        /// <summary>
        ///   Gets or sets the custom text of the "Paste" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null), Localizable(true)]
        public string PasteMenuItemText { get; set; }

        /// <summary>
        ///   Gets or sets the custom text of the "Select All" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null), Localizable(true)]
        public string SelectAllMenuItemText { get; set; }

        /// <summary>
        ///   Gets the text of the "Cut" ContextMenuStrip item.
        /// </summary>
        internal string CutMenuItemTextInternal {
            get { return !string.IsNullOrEmpty(CutMenuItemText) ? CutMenuItemText : "Cut"; }
        }

        /// <summary>
        ///   Gets the text of the "Copy" ContextMenuStrip item.
        /// </summary>
        internal string CopyMenuItemTextInternal {
            get { return !string.IsNullOrEmpty(CopyMenuItemText) ? CopyMenuItemText : "Copy"; }
        }

        /// <summary>
        ///   Gets the text of the "Paste" ContextMenuStrip item.
        /// </summary>
        internal string PasteMenuItemTextInternal {
            get { return !string.IsNullOrEmpty(PasteMenuItemText) ? PasteMenuItemText : "Paste"; }
        }

        /// <summary>
        ///   Gets the text of the "Select All" ContextMenuStrip item.
        /// </summary>
        internal string SelectAllMenuItemTextInternal {
            get { return !string.IsNullOrEmpty(SelectAllMenuItemText) ? SelectAllMenuItemText : "SelectAll"; }
        }

        /// <summary>
        ///   Gets or sets the image of the "Cut" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null)]
        public Image CutMenuItemImage { get; set; }

        /// <summary>
        ///   Gets or sets the image of the "Copy" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null)]
        public Image CopyMenuItemImage { get; set; }

        /// <summary>
        ///   Gets or sets the image of the "Paste" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null)]
        public Image PasteMenuItemImage { get; set; }

        /// <summary>
        ///   Gets or sets the image of the "Select All" ContextMenuStrip item.
        /// </summary>
        [Category("BuiltIn-ContextMenu"), DefaultValue(null)]
        public Image SelectAllMenuItemImage { get; set; }
    }
}