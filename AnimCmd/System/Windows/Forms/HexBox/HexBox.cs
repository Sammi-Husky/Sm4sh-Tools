using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Timer = System.Windows.Forms.Timer;

namespace Be.Windows.Forms
{
    /// <summary>
    ///   Represents a hex box control.
    /// </summary>
    [ToolboxBitmap(typeof(HexBox), "HexBox.bmp")]
    public class HexBox : Control
    {
        #region IKeyInterpreter interface
        /// <summary>
        ///   Defines a user input handler such as for mouse and keyboard input
        /// </summary>
        private interface IKeyInterpreter
        {
            /// <summary>
            ///   Activates mouse events
            /// </summary>
            void Activate();

            /// <summary>
            ///   Deactivate mouse events
            /// </summary>
            void Deactivate();

            /// <summary>
            ///   Preprocesses WM_KEYUP window message.
            /// </summary>
            /// <param name = "m">the Message object to process.</param>
            /// <returns>True, if the message was processed.</returns>
            bool PreProcessWmKeyUp(ref Message m);

            /// <summary>
            ///   Preprocesses WM_CHAR window message.
            /// </summary>
            /// <param name = "m">the Message object to process.</param>
            /// <returns>True, if the message was processed.</returns>
            bool PreProcessWmChar(ref Message m);

            /// <summary>
            ///   Preprocesses WM_KEYDOWN window message.
            /// </summary>
            /// <param name = "m">the Message object to process.</param>
            /// <returns>True, if the message was processed.</returns>
            bool PreProcessWmKeyDown(ref Message m);

            /// <summary>
            ///   Gives some information about where to place the caret.
            /// </summary>
            /// <param name = "byteIndex">the index of the byte</param>
            /// <returns>the position where the caret is to place.</returns>
            PointF GetCaretPointF(long byteIndex);
        }
        #endregion

        #region EmptyKeyInterpreter class
        /// <summary>
        ///   Represents an empty input handler without any functionality. 
        ///   If is set ByteProvider to null, then this interpreter is used.
        /// </summary>
        private class EmptyKeyInterpreter : IKeyInterpreter
        {
            private readonly HexBox _hexBox;

            public EmptyKeyInterpreter(HexBox hexBox)
            {
                _hexBox = hexBox;
            }

            #region IKeyInterpreter Members
            public void Activate() { }
            public void Deactivate() { }

            public bool PreProcessWmKeyUp(ref Message m)
            {
                return _hexBox.BasePreProcessMessage(ref m);
            }

            public bool PreProcessWmChar(ref Message m)
            {
                return _hexBox.BasePreProcessMessage(ref m);
            }

            public bool PreProcessWmKeyDown(ref Message m)
            {
                return _hexBox.BasePreProcessMessage(ref m);
            }

            public PointF GetCaretPointF(long byteIndex)
            {
                return new PointF();
            }
            #endregion
        }
        #endregion

        #region KeyInterpreter class
        /// <summary>
        ///   Handles user input such as mouse and keyboard input during hex view edit
        /// </summary>
        private class KeyInterpreter : IKeyInterpreter
        {
            #region Fields
            /// <summary>
            ///   Contains the parent HexBox control
            /// </summary>
            protected readonly HexBox HexBox;

            /// <summary>
            ///   Contains True, if shift key is down
            /// </summary>
            private bool _shiftDown;

            /// <summary>
            ///   Contains True, if mouse is down
            /// </summary>
            private bool _mouseDown;

            /// <summary>
            ///   Contains the selection start position info
            /// </summary>
            private BytePositionInfo _bpiStart;

            /// <summary>
            ///   Contains the current mouse selection position info
            /// </summary>
            private BytePositionInfo _bpi;
            #endregion

            #region Ctors
            public KeyInterpreter(HexBox hexBox)
            {
                HexBox = hexBox;
            }
            #endregion

            #region Activate, Deactive methods
            public virtual void Activate()
            {
                HexBox.MouseDown += BeginMouseSelection;
                HexBox.MouseMove += UpdateMouseSelection;
                HexBox.MouseUp += EndMouseSelection;
                HexBox.KeyDown += KeyDownEventHL;
            }

            public virtual void Deactivate()
            {
                HexBox.MouseDown -= BeginMouseSelection;
                HexBox.MouseMove -= UpdateMouseSelection;
                HexBox.MouseUp -= EndMouseSelection;
            }
            #endregion

            private void KeyDownEventHL(object sender, KeyEventArgs e)
            {
                //TODO Highlight??
                var m = new Message { WParam = (IntPtr)e.KeyData };
                PreProcessWmKeyDown(ref m);
            }

            #region Mouse selection methods
            private void BeginMouseSelection(object sender, MouseEventArgs e)
            {
                Debug.WriteLine("BeginMouseSelection()", "KeyInterpreter");
                if (e.Button != MouseButtons.Left)
                    return;
                _mouseDown = true;
                if (!_shiftDown)
                {
                    _bpiStart = new BytePositionInfo(HexBox._bytePos, HexBox._byteCharacterPos);
                    HexBox.ReleaseSelection();
                }
                else UpdateMouseSelection(this, e);
            }

            private void UpdateMouseSelection(object sender, MouseEventArgs e)
            {
                if (!_mouseDown)
                    return;
                _bpi = GetBytePositionInfo(new Point(e.X, e.Y));
                var selEnd = _bpi.Index;
                long realselStart;
                long realselLength;
                if (selEnd < _bpiStart.Index)
                {
                    realselStart = selEnd;
                    realselLength = _bpiStart.Index - selEnd;
                }
                else if (selEnd > _bpiStart.Index)
                {
                    realselStart = _bpiStart.Index;
                    realselLength = selEnd - realselStart;
                }
                else
                {
                    realselStart = HexBox._bytePos;
                    realselLength = 0;
                }
                if (realselStart == HexBox._bytePos && realselLength == HexBox._selectionLength) return;
                HexBox.InternalSelect(realselStart, realselLength);
                HexBox.ScrollByteIntoView(_bpi.Index); // <--- This line is added
            }

            private void EndMouseSelection(object sender, MouseEventArgs e)
            {
                _mouseDown = false;
            }
            #endregion

            #region PrePrcessWmKeyDown methods
            public virtual bool PreProcessWmKeyDown(ref Message m)
            {
                Debug.WriteLine("PreProcessWmKeyDown(ref Message m)", "KeyInterpreter");
                /*
                switch (keyData) {
                    case Keys.Left:
                    case Keys.Up:
                    case Keys.Right:
                    case Keys.Down:
                    case Keys.PageUp:
                    case Keys.PageDown:
                    case Keys.Left | Keys.Shift:
                    case Keys.Up | Keys.Shift:
                    case Keys.Right | Keys.Shift:
                    case Keys.Down | Keys.Shift:
                    case Keys.Tab:
                    case Keys.Back:
                    case Keys.Delete:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.ShiftKey | Keys.Shift:
                    case Keys.C | Keys.Control:
                    case Keys.X | Keys.Control:
                    case Keys.V | Keys.Control:
                        if (RaiseKeyDown(keyData))
                            return true;
                        break;
                }
                 */
                switch ((Keys)m.WParam.ToInt32() | ModifierKeys)
                {
                    case Keys.Left: // move left
                        return PreProcessWmKeyDownLeft(ref m);
                    case Keys.Up: // move up
                        return PreProcessWmKeyDownUp(ref m);
                    case Keys.Right: // move right
                        return PreProcessWmKeyDownRight(ref m);
                    case Keys.Down: // move down
                        return PreProcessWmKeyDownDown(ref m);
                    case Keys.PageUp: // move pageup
                        return PreProcessWmKeyDownPageUp(ref m);
                    case Keys.PageDown: // move pagedown
                        return PreProcessWmKeyDownPageDown(ref m);
                    case Keys.Left | Keys.Shift: // move left with selection
                        return PreProcessWmKeyDownShiftLeft(ref m);
                    case Keys.Up | Keys.Shift: // move up with selection
                        return PreProcessWmKeyDownShiftUp(ref m);
                    case Keys.Right | Keys.Shift: // move right with selection
                        return PreProcessWmKeyDownShiftRight(ref m);
                    case Keys.Down | Keys.Shift: // move down with selection
                        return PreProcessWmKeyDownShiftDown(ref m);
                    case Keys.Tab: // switch focus to string view
                        return PreProcessWmKeyDownTab(ref m);
                    case Keys.Back: // back
                        return PreProcessWmKeyDownBack(ref m);
                    case Keys.Delete: // delete
                        return PreProcessWmKeyDownDelete(ref m);
                    case Keys.Home: // move to home
                        return PreProcessWmKeyDownHome(ref m);
                    case Keys.End: // move to end
                        return PreProcessWmKeyDownEnd(ref m);
                    case Keys.ShiftKey | Keys.Shift: // begin selection process
                        return PreProcessWmKeyDownShiftShiftKey(ref m);
                    case Keys.C | Keys.Control: // copy
                        return PreProcessWmKeyDownControlC(ref m);
                    case Keys.X | Keys.Control: // cut
                        return PreProcessWmKeyDownControlX(ref m);
                    case Keys.V | Keys.Control: // paste
                        return PreProcessWmKeyDownControlV(ref m);
                    default:
                        HexBox.ScrollByteIntoView();
                        return PreProcessWmChar(ref m);
                }
            }

            protected bool RaiseKeyDown(Keys keyData)
            {
                var e = new KeyEventArgs(keyData);
                HexBox.OnKeyDown(e);
                return e.Handled;
            }

            protected virtual bool PreProcessWmKeyDownLeft(ref Message m)
            {
                return PerformPosMoveLeft();
            }

            protected virtual bool PreProcessWmKeyDownUp(ref Message m)
            {
                var pos = HexBox._bytePos;
                var cp = HexBox._byteCharacterPos;
                if (!(pos == 0 && cp == 0))
                {
                    pos = Math.Max(-1, pos - HexBox.HorizontalByteCount);
                    if (pos == -1)
                        return true;
                    HexBox.SetPosition(pos);
                    if (pos < HexBox._startByte) HexBox.PerformScrollLineUp();
                    HexBox.UpdateCaret();
                    HexBox.Invalidate();
                }
                HexBox.ScrollByteIntoView();
                HexBox.ReleaseSelection();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownRight(ref Message m)
            {
                return PerformPosMoveRight();
            }

            protected virtual bool PreProcessWmKeyDownDown(ref Message m)
            {
                var pos = HexBox._bytePos;
                var cp = HexBox._byteCharacterPos;
                if (pos == HexBox._byteProvider.Length && cp == 0)
                    return true;
                pos = Math.Min(HexBox._byteProvider.Length, pos + HexBox.HorizontalByteCount);
                if (pos == HexBox._byteProvider.Length)
                    cp = 0;
                HexBox.SetPosition(pos, cp);
                if (pos > HexBox._endByte - 1) HexBox.PerformScrollLineDown();
                HexBox.UpdateCaret();
                HexBox.ScrollByteIntoView();
                HexBox.ReleaseSelection();
                HexBox.Invalidate();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownPageUp(ref Message m)
            {
                var pos = HexBox._bytePos;
                var cp = HexBox._byteCharacterPos;
                if (pos == 0 && cp == 0)
                    return true;
                pos = Math.Max(0, pos - HexBox._iHexMaxBytes);
                if (pos == 0)
                    return true;
                HexBox.SetPosition(pos);
                if (pos < HexBox._startByte) HexBox.PerformScrollPageUp();
                HexBox.ReleaseSelection();
                HexBox.UpdateCaret();
                HexBox.Invalidate();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownPageDown(ref Message m)
            {
                var pos = HexBox._bytePos;
                var cp = HexBox._byteCharacterPos;
                if (pos == HexBox._byteProvider.Length && cp == 0)
                    return true;
                pos = Math.Min(HexBox._byteProvider.Length, pos + HexBox._iHexMaxBytes);
                if (pos == HexBox._byteProvider.Length)
                    cp = 0;
                HexBox.SetPosition(pos, cp);
                if (pos > HexBox._endByte - 1) HexBox.PerformScrollPageDown();
                HexBox.ReleaseSelection();
                HexBox.UpdateCaret();
                HexBox.Invalidate();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownShiftLeft(ref Message m)
            {
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                if (pos + sel < 1)
                    return true;
                if (pos + sel <= _bpiStart.Index)
                {
                    if (pos == 0)
                        return true;
                    pos--;
                    sel++;
                }
                else sel = Math.Max(0, sel - 1);
                HexBox.ScrollByteIntoView();
                HexBox.InternalSelect(pos, sel);
                return true;
            }

            protected virtual bool PreProcessWmKeyDownShiftUp(ref Message m)
            {
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                if (pos - HexBox.HorizontalByteCount < 0 && pos <= _bpiStart.Index)
                    return true;
                if (_bpiStart.Index >= pos + sel)
                {
                    pos = pos - HexBox.HorizontalByteCount;
                    sel += HexBox.HorizontalByteCount;
                    HexBox.InternalSelect(pos, sel);
                    HexBox.ScrollByteIntoView();
                }
                else
                {
                    sel -= HexBox.HorizontalByteCount;
                    if (sel < 0)
                    {
                        pos = _bpiStart.Index + sel;
                        sel = -sel;
                        HexBox.InternalSelect(pos, sel);
                        HexBox.ScrollByteIntoView();
                    }
                    else
                    {
                        sel -= HexBox.HorizontalByteCount;
                        HexBox.InternalSelect(pos, sel);
                        HexBox.ScrollByteIntoView(pos + sel);
                    }
                }
                return true;
            }

            protected virtual bool PreProcessWmKeyDownShiftRight(ref Message m)
            {
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                if (pos + sel >= HexBox._byteProvider.Length)
                    return true;
                if (_bpiStart.Index <= pos)
                {
                    sel++;
                    HexBox.InternalSelect(pos, sel);
                    HexBox.ScrollByteIntoView(pos + sel);
                }
                else
                {
                    pos++;
                    sel = Math.Max(0, sel - 1);
                    HexBox.InternalSelect(pos, sel);
                    HexBox.ScrollByteIntoView();
                }
                return true;
            }

            protected virtual bool PreProcessWmKeyDownShiftDown(ref Message m)
            {
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                var max = HexBox._byteProvider.Length;
                if (pos + sel + HexBox.HorizontalByteCount > max)
                    return true;
                if (_bpiStart.Index <= pos)
                {
                    sel += HexBox.HorizontalByteCount;
                    HexBox.InternalSelect(pos, sel);
                    HexBox.ScrollByteIntoView(pos + sel);
                }
                else
                {
                    sel -= HexBox.HorizontalByteCount;
                    if (sel < 0)
                    {
                        pos = _bpiStart.Index;
                        sel = -sel;
                    }
                    else pos += HexBox.HorizontalByteCount;
                    //sel -= _hexBox._iHexMaxHBytes;
                    HexBox.InternalSelect(pos, sel);
                    HexBox.ScrollByteIntoView();
                }
                return true;
            }

            protected virtual bool PreProcessWmKeyDownTab(ref Message m)
            {
                if (HexBox._stringViewVisible && HexBox._keyInterpreter.GetType() == typeof(KeyInterpreter))
                {
                    HexBox.ActivateStringKeyInterpreter();
                    HexBox.ScrollByteIntoView();
                    HexBox.ReleaseSelection();
                    HexBox.UpdateCaret();
                    HexBox.Invalidate();
                    return true;
                }
                if (HexBox.Parent == null) return true;
                HexBox.Parent.SelectNextControl(HexBox, true, true, true, true);
                return true;
            }

            protected virtual bool PreProcessWmKeyDownShiftTab(ref Message m)
            {
                if (HexBox._keyInterpreter is StringKeyInterpreter)
                {
                    _shiftDown = false;
                    HexBox.ActivateKeyInterpreter();
                    HexBox.ScrollByteIntoView();
                    HexBox.ReleaseSelection();
                    HexBox.UpdateCaret();
                    HexBox.Invalidate();
                    return true;
                }
                if (HexBox.Parent == null) return true;
                HexBox.Parent.SelectNextControl(HexBox, false, true, true, true);
                return true;
            }

            protected virtual bool PreProcessWmKeyDownBack(ref Message m)
            {
                if (!HexBox._byteProvider.SupportsDeleteBytes())
                    return true;
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                var cp = HexBox._byteCharacterPos;
                var startDelete = (cp == 0 && sel == 0) ? pos - 1 : pos;
                if (startDelete < 0 && sel < 1)
                    return true;
                var bytesToDelete = (sel > 0) ? sel : 1;
                HexBox._byteProvider.DeleteBytes(Math.Max(0, startDelete), bytesToDelete);
                HexBox.UpdateScrollSize();
                if (sel == 0)
                    PerformPosMoveLeftByte();
                HexBox.ReleaseSelection();
                HexBox.Invalidate();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownDelete(ref Message m)
            {
                if (!HexBox._byteProvider.SupportsDeleteBytes())
                    return true;
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                if (pos >= HexBox._byteProvider.Length)
                    return true;
                var bytesToDelete = (sel > 0) ? sel : 1;
                HexBox._byteProvider.DeleteBytes(pos, bytesToDelete);
                HexBox.UpdateScrollSize();
                HexBox.ReleaseSelection();
                HexBox.Invalidate();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownHome(ref Message m)
            {
                if (HexBox._bytePos < 1) return true;
                HexBox.SetPosition(0, 0);
                HexBox.ScrollByteIntoView();
                HexBox.UpdateCaret();
                HexBox.ReleaseSelection();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownEnd(ref Message m)
            {
                var length = HexBox._byteProvider.Length;
                if (HexBox._bytePos >= length - 1)
                    return true;
                HexBox.SetPosition(length, 0);
                HexBox.ScrollByteIntoView();
                HexBox.UpdateCaret();
                HexBox.ReleaseSelection();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownShiftShiftKey(ref Message m)
            {
                //TODO UGHHHH
                if (_mouseDown)
                    return true;
                if (_shiftDown)
                    return true;
                _shiftDown = true;
                if (HexBox._selectionLength > 0)
                    return true;
                _bpiStart = new BytePositionInfo(HexBox._bytePos, HexBox._byteCharacterPos);
                return true;
            }

            protected virtual bool PreProcessWmKeyDownControlC(ref Message m)
            {
                HexBox.Copy();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownControlX(ref Message m)
            {
                HexBox.Cut();
                return true;
            }

            protected virtual bool PreProcessWmKeyDownControlV(ref Message m)
            {
                HexBox.Paste();
                return true;
            }
            #endregion

            #region PreProcessWmChar methods
            public virtual bool PreProcessWmChar(ref Message m)
            {
                if (ModifierKeys == Keys.Control) return HexBox.BasePreProcessMessage(ref m);
                var sw = HexBox._byteProvider.SupportsWriteByte();
                var si = HexBox._byteProvider.SupportsInsertBytes();
                var sd = HexBox._byteProvider.SupportsDeleteBytes();
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                var cp = HexBox._byteCharacterPos;
                if (
                    (!sw) ||
                    (!si && pos == HexBox._byteProvider.Length)) return HexBox.BasePreProcessMessage(ref m);
                var c = (char)m.WParam.ToInt32();
                if (!Uri.IsHexDigit(c)) return HexBox.BasePreProcessMessage(ref m);
                if (RaiseKeyPress(c) || HexBox.ReadOnly) return true;
                var isInsertMode = pos == HexBox._byteProvider.Length;
                // do insert when insertActive = true
                if (!isInsertMode && si && HexBox.InsertActive && cp == 0)
                    isInsertMode = true;
                if (sd && si && sel > 0)
                {
                    HexBox._byteProvider.DeleteBytes(pos, sel);
                    isInsertMode = true;
                    cp = 0;
                    HexBox.SetPosition(pos, cp);
                }
                HexBox.ReleaseSelection();
                var currentByte = isInsertMode ? (byte)0 : HexBox._byteProvider.ReadByte(pos);
                var sCb = currentByte.ToString("X", Thread.CurrentThread.CurrentCulture);
                if (sCb.Length == 1) sCb = "0" + sCb;
                var sNewCb = c.ToString();
                if (cp == 0)
                    sNewCb += sCb.Substring(1, 1);
                else
                    sNewCb = sCb.Substring(0, 1) + sNewCb;

                var newcb = byte.Parse(sNewCb, NumberStyles.AllowHexSpecifier,
                                       Thread.CurrentThread.CurrentCulture);
                if (isInsertMode)
                    HexBox._byteProvider.InsertBytes(pos, new[] { newcb });
                else
                    HexBox._byteProvider.WriteByte(pos, newcb);
                PerformPosMoveRight();
                HexBox.Invalidate();
                return true;
            }

            protected bool RaiseKeyPress(char keyChar)
            {
                var e = new KeyPressEventArgs(keyChar);
                HexBox.OnKeyPress(e);
                return e.Handled;
            }
            #endregion

            #region PreProcessWmKeyUp methods
            public virtual bool PreProcessWmKeyUp(ref Message m)
            {
                Debug.WriteLine("PreProcessWmKeyUp(ref Message m)", "KeyInterpreter");
                var keyData = (Keys)m.WParam.ToInt32() | ModifierKeys;
                switch (keyData)
                {
                    case Keys.ShiftKey:
                    case Keys.Insert:
                        if (RaiseKeyUp(keyData))
                            return true;
                        break;
                }
                switch (keyData)
                {
                    case Keys.ShiftKey:
                        _shiftDown = false;
                        return true;
                    case Keys.Insert:
                        return PreProcessWmKeyUpInsert(ref m);
                    default:
                        return HexBox.BasePreProcessMessage(ref m);
                }
            }

            protected virtual bool PreProcessWmKeyUpInsert(ref Message m)
            {
                HexBox.InsertActive = !HexBox.InsertActive;
                return true;
            }

            private bool RaiseKeyUp(Keys keyData)
            {
                var e = new KeyEventArgs(keyData);
                HexBox.OnKeyUp(e);
                return e.Handled;
            }
            #endregion

            #region Misc
            protected virtual bool PerformPosMoveLeft()
            {
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                var cp = HexBox._byteCharacterPos;
                if (sel != 0)
                {
                    cp = 0;
                    HexBox.SetPosition(pos, cp);
                    HexBox.ReleaseSelection();
                }
                else
                {
                    if (pos == 0 && cp == 0)
                        return true;
                    if (cp > 0) cp--;
                    else
                    {
                        pos = Math.Max(0, pos - 1);
                        cp++;
                    }
                    HexBox.SetPosition(pos, cp);
                    if (pos < HexBox._startByte) HexBox.PerformScrollLineUp();
                    HexBox.UpdateCaret();
                    HexBox.Invalidate();
                }
                HexBox.ScrollByteIntoView();
                return true;
            }

            protected virtual bool PerformPosMoveRight()
            {
                var pos = HexBox._bytePos;
                var cp = HexBox._byteCharacterPos;
                var sel = HexBox._selectionLength;
                if (sel != 0)
                {
                    pos += sel;
                    cp = 0;
                    HexBox.SetPosition(pos, cp);
                    HexBox.ReleaseSelection();
                }
                else if (!(pos == HexBox._byteProvider.Length && cp == 0))
                {
                    if (cp > 0)
                    {
                        pos = Math.Min(HexBox._byteProvider.Length, pos + 1);
                        cp = 0;
                    }
                    else cp++;
                    HexBox.SetPosition(pos, cp);
                    if (pos > HexBox._endByte - 1) HexBox.PerformScrollLineDown();
                    HexBox.UpdateCaret();
                    HexBox.Invalidate();
                }
                HexBox.ScrollByteIntoView();
                return true;
            }

            protected virtual bool PerformPosMoveLeftByte()
            {
                var pos = HexBox._bytePos;
                if (pos == 0) return true;
                pos = Math.Max(0, pos - 1);
                HexBox.SetPosition(pos, 0);
                if (pos < HexBox._startByte) HexBox.PerformScrollLineUp();
                HexBox.UpdateCaret();
                HexBox.ScrollByteIntoView();
                HexBox.Invalidate();
                return true;
            }

            protected virtual bool PerformPosMoveRightByte()
            {
                var pos = HexBox._bytePos;
                if (pos == HexBox._byteProvider.Length)
                    return true;
                pos = Math.Min(HexBox._byteProvider.Length, pos + 1);
                HexBox.SetPosition(pos, 0);
                if (pos > HexBox._endByte - 1) HexBox.PerformScrollLineDown();
                HexBox.UpdateCaret();
                HexBox.ScrollByteIntoView();
                HexBox.Invalidate();
                return true;
            }

            public virtual PointF GetCaretPointF(long byteIndex)
            {
                Debug.WriteLine("GetCaretPointF()", "KeyInterpreter");
                return HexBox.GetBytePointF(byteIndex);
            }

            protected virtual BytePositionInfo GetBytePositionInfo(Point p)
            {
                return HexBox.GetHexBytePositionInfo(p);
            }
            #endregion
        }
        #endregion

        #region StringKeyInterpreter class
        /// <summary>
        ///   Handles user input such as mouse and keyboard input during string view edit
        /// </summary>
        private class StringKeyInterpreter : KeyInterpreter
        {
            #region Ctors
            public StringKeyInterpreter(HexBox hexBox)
                : base(hexBox)
            {
                HexBox._byteCharacterPos = 0;
            }
            #endregion

            #region PreProcessWmKeyDown methods
            public override bool PreProcessWmKeyDown(ref Message m)
            {
                var vc = (Keys)m.WParam.ToInt32();
                var keyData = vc | ModifierKeys;
                switch (keyData)
                {
                    case Keys.Tab | Keys.Shift:
                    case Keys.Tab:
                        if (RaiseKeyDown(keyData))
                            return true;
                        break;
                }
                switch (keyData)
                {
                    case Keys.Tab | Keys.Shift:
                        return PreProcessWmKeyDownShiftTab(ref m);
                    case Keys.Tab:
                        return PreProcessWmKeyDownTab(ref m);
                    default:
                        return base.PreProcessWmKeyDown(ref m);
                }
            }

            protected override bool PreProcessWmKeyDownLeft(ref Message m)
            {
                return PerformPosMoveLeftByte();
            }

            protected override bool PreProcessWmKeyDownRight(ref Message m)
            {
                return PerformPosMoveRightByte();
            }
            #endregion

            #region PreProcessWmChar methods
            public override bool PreProcessWmChar(ref Message m)
            {
                if (ModifierKeys == Keys.Control) return HexBox.BasePreProcessMessage(ref m);
                var sw = HexBox._byteProvider.SupportsWriteByte();
                var si = HexBox._byteProvider.SupportsInsertBytes();
                var sd = HexBox._byteProvider.SupportsDeleteBytes();
                var pos = HexBox._bytePos;
                var sel = HexBox._selectionLength;
                if (
                    (!sw && pos != HexBox._byteProvider.Length) ||
                    (!si && pos == HexBox._byteProvider.Length)) return HexBox.BasePreProcessMessage(ref m);
                var c = (char)m.WParam.ToInt32();
                if (RaiseKeyPress(c))
                    return true;
                if (HexBox.ReadOnly)
                    return true;
                var isInsertMode = (pos == HexBox._byteProvider.Length);
                // do insert when insertActive = true
                if (!isInsertMode && si && HexBox.InsertActive)
                    isInsertMode = true;
                if (sd && si && sel > 0)
                {
                    HexBox._byteProvider.DeleteBytes(pos, sel);
                    isInsertMode = true;
                    HexBox.SetPosition(pos, 0);
                }
                HexBox.ReleaseSelection();
                if (isInsertMode)
                    HexBox._byteProvider.InsertBytes(pos, new[] { (byte)c });
                else
                    HexBox._byteProvider.WriteByte(pos, (byte)c);
                PerformPosMoveRightByte();
                HexBox.Invalidate();
                return true;
            }
            #endregion

            #region Misc
            public override PointF GetCaretPointF(long byteIndex)
            {
                Debug.WriteLine("GetCaretPointF()", "StringKeyInterpreter");
                var gp = HexBox.GetGridBytePoint(byteIndex, true);
                return HexBox.GetByteStringPointF(gp);
            }

            protected override BytePositionInfo GetBytePositionInfo(Point p)
            {
                return HexBox.GetStringBytePositionInfo(p);
            }
            #endregion
        }
        #endregion

        #region Fields
        /// <summary>
        ///   Contains the hole content bounds of all text
        /// </summary>
        private Rectangle _recContent;

        /// <summary>
        ///   Contains the line info bounds
        /// </summary>
        private Rectangle _recLineInfo;

        /// <summary>
        /// Contains the column info header rectangle bounds
        /// </summary>
        Rectangle _recColumnInfo;

        /// <summary>
        ///   Contains the hex data bounds
        /// </summary>
        private Rectangle _recHex;

        /// <summary>
        ///   Contains the string view bounds
        /// </summary>
        private Rectangle _recStringView;

        /// <summary>
        ///   Contains string format information for text drawing
        /// </summary>
        private readonly StringFormat _stringFormat;

        /// <summary>
        ///   Contains the width and height of a single char
        /// </summary>
        private SizeF _charSize;

        /// <summary>
        ///   Contains the maximum of visible bytes.
        /// </summary>
        private int _iHexMaxBytes;

        /// <summary>
        ///   Contains the scroll bars minimum value
        /// </summary>
        private long _scrollVmin;

        /// <summary>
        ///   Contains the scroll bars maximum value
        /// </summary>
        private long _scrollVmax;

        /// <summary>
        ///   Contains the scroll bars current position
        /// </summary>
        private long _scrollVpos;

        /// <summary>
        ///   Contains a vertical scroll
        /// </summary>
        private readonly VScrollBar _vScrollBar;

        /// <summary>
        ///   Contains a timer for thumbtrack scrolling
        /// </summary>
        private readonly Timer _thumbTrackTimer = new Timer();

        /// <summary>
        ///   Contains the thumbtrack scrolling position
        /// </summary>
        private long _thumbTrackPosition;

        /// <summary>
        ///   Contains the thumptrack delay for scrolling in milliseconds.
        /// </summary>
        private const int THUMPTRACKDELAY = 50;

        /// <summary>
        ///   Contains the Enviroment.TickCount of the last refresh
        /// </summary>
        private int _lastThumbtrack;

        /// <summary>
        ///   Contains the border큦 left shift
        /// </summary>
        private int _recBorderLeft = SystemInformation.Border3DSize.Width;

        /// <summary>
        ///   Contains the border큦 right shift
        /// </summary>
        private int _recBorderRight = SystemInformation.Border3DSize.Width;

        /// <summary>
        ///   Contains the border큦 top shift
        /// </summary>
        private int _recBorderTop = SystemInformation.Border3DSize.Height;

        /// <summary>
        ///   Contains the border bottom shift
        /// </summary>
        private int _recBorderBottom = SystemInformation.Border3DSize.Height;

        /// <summary>
        ///   Contains the index of the first visible byte
        /// </summary>
        private long _startByte;

        /// <summary>
        ///   Contains the index of the last visible byte
        /// </summary>
        private long _endByte;

        /// <summary>
        ///   Contains the current byte position
        /// </summary>
        public long _bytePos = -1;

        /// <summary>
        ///   Contains the current char position in one byte
        /// </summary>
        /// <example>
        ///   "1A"
        ///   "1" = char position of 0
        ///   "A" = char position of 1
        /// </example>
        private int _byteCharacterPos;

        /// <summary>
        ///   Contains string format information for hex values
        /// </summary>
        private string _hexStringFormat = "X";

        /// <summary>
        ///   Contains the current key interpreter
        /// </summary>
        private IKeyInterpreter _keyInterpreter;

        /// <summary>
        ///   Contains an empty key interpreter without functionality
        /// </summary>
        private EmptyKeyInterpreter _eki;

        /// <summary>
        ///   Contains the default key interpreter
        /// </summary>
        private KeyInterpreter _ki;

        /// <summary>
        ///   Contains the string key interpreter
        /// </summary>
        private StringKeyInterpreter _ski;

        /// <summary>
        ///   Contains True if caret is visible
        /// </summary>
        private bool _caretVisible;

        /// <summary>
        ///   Contains true, if the find (Find method) should be aborted.
        /// </summary>
        private bool _abortFind;

        /// <summary>
        ///   Contains a state value about Insert or Write mode. When this value is true and the ByteProvider SupportsInsert is true bytes are inserted instead of overridden.
        /// </summary>
        private bool _insertActive;
        #endregion

        #region Events
        /// <summary>
        ///   Occurs, when the value of InsertActive property has changed.
        /// </summary>
        [Description("Occurs, when the value of InsertActive property has changed.")]
        public event EventHandler InsertActiveChanged;

        /// <summary>
        ///   Occurs, when the value of ReadOnly property has changed.
        /// </summary>
        [Description("Occurs, when the value of ReadOnly property has changed.")]
        public event EventHandler ReadOnlyChanged;

        /// <summary>
        ///   Occurs, when the value of ByteProvider property has changed.
        /// </summary>
        [Description("Occurs, when the value of ByteProvider property has changed.")]
        public event EventHandler ByteProviderChanged;

        /// <summary>
        ///   Occurs, when the value of SelectionStart property has changed.
        /// </summary>
        [Description("Occurs, when the value of SelectionStart property has changed.")]
        public event EventHandler SelectionStartChanged;

        /// <summary>
        ///   Occurs, when the value of SelectionLength property has changed.
        /// </summary>
        [Description("Occurs, when the value of SelectionLength property has changed.")]
        public event EventHandler SelectionLengthChanged;

        /// <summary>
        ///   Occurs, when the value of LineInfoVisible property has changed.
        /// </summary>
        [Description("Occurs, when the value of LineInfoVisible property has changed.")]
        public event EventHandler LineInfoVisibleChanged;

        /// <summary>
        ///   Occurs, when the value of StringViewVisible property has changed.
        /// </summary>
        [Description("Occurs, when the value of StringViewVisible property has changed.")]
        public event EventHandler StringViewVisibleChanged;

        /// <summary>
        ///   Occurs, when the value of BorderStyle property has changed.
        /// </summary>
        [Description("Occurs, when the value of BorderStyle property has changed.")]
        public event EventHandler BorderStyleChanged;

        /// <summary>
        ///   Occurs, when the value of BytesPerLine property has changed.
        /// </summary>
        [Description("Occurs, when the value of BytesPerLine property has changed.")]
        public event EventHandler BytesPerLineChanged;

        /// <summary>
        ///   Occurs, when the value of UseFixedBytesPerLine property has changed.
        /// </summary>
        [Description("Occurs, when the value of UseFixedBytesPerLine property has changed.")]
        public event EventHandler UseFixedBytesPerLineChanged;

        /// <summary>
        ///   Occurs, when the value of VScrollBarVisible property has changed.
        /// </summary>
        [Description("Occurs, when the value of VScrollBarVisible property has changed.")]
        public event EventHandler VScrollBarVisibleChanged;

        /// <summary>
        ///   Occurs, when the value of HexCasing property has changed.
        /// </summary>
        [Description("Occurs, when the value of HexCasing property has changed.")]
        public event EventHandler HexCasingChanged;

        /// <summary>
        ///   Occurs, when the value of HorizontalByteCount property has changed.
        /// </summary>
        [Description("Occurs, when the value of HorizontalByteCount property has changed.")]
        public event EventHandler HorizontalByteCountChanged;

        /// <summary>
        ///   Occurs, when the value of VerticalByteCount property has changed.
        /// </summary>
        [Description("Occurs, when the value of VerticalByteCount property has changed.")]
        public event EventHandler VerticalByteCountChanged;

        /// <summary>
        ///   Occurs, when the value of CurrentLine property has changed.
        /// </summary>
        [Description("Occurs, when the value of CurrentLine property has changed.")]
        public event EventHandler CurrentLineChanged;

        /// <summary>
        ///   Occurs, when the value of CurrentPositionInLine property has changed.
        /// </summary>
        [Description("Occurs, when the value of CurrentPositionInLine property has changed.")]
        public event EventHandler CurrentPositionInLineChanged;

        /// <summary>
        ///   Occurs, when Copy method was invoked and ClipBoardData changed.
        /// </summary>
        [Description("Occurs, when Copy method was invoked and ClipBoardData changed.")]
        public event EventHandler Copied;

        /// <summary>
        ///   Occurs, when CopyHex method was invoked and ClipBoardData changed.
        /// </summary>
        [Description("Occurs, when CopyHex method was invoked and ClipBoardData changed.")]
        public event EventHandler CopiedHex;
        #endregion

        #region Ctors
        /// <summary>
        ///   Initializes a new instance of a HexBox class.
        /// </summary>
        public HexBox()
        {
            _highlights = new List<HexboxHighlight>();
            _vScrollBar = new VScrollBar();
            _vScrollBar.Scroll += VScrollBarScroll;
            _builtInContextMenu = new BuiltInContextMenu(this);
            base.BackColor = Color.White;
            base.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, ((0)));
            _stringFormat =
                new StringFormat(StringFormat.GenericTypographic)
                {
                    FormatFlags = StringFormatFlags.MeasureTrailingSpaces
                };
            ActivateEmptyKeyInterpreter();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            _thumbTrackTimer.Interval = 50;
            _thumbTrackTimer.Tick += PerformScrollThumbTrack;
        }
        #endregion

        #region Scroll methods
        private void VScrollBarScroll(object sender, ScrollEventArgs e)
        {
            switch (e.Type)
            {
                case ScrollEventType.Last:
                    break;
                case ScrollEventType.EndScroll:
                    break;
                case ScrollEventType.SmallIncrement:
                    PerformScrollLineDown();
                    break;
                case ScrollEventType.SmallDecrement:
                    PerformScrollLineUp();
                    break;
                case ScrollEventType.LargeIncrement:
                    PerformScrollPageDown();
                    break;
                case ScrollEventType.LargeDecrement:
                    PerformScrollPageUp();
                    break;
                case ScrollEventType.ThumbPosition:
                    var lPos = FromScrollPos(e.NewValue);
                    PerformScrollThumpPosition(lPos);
                    break;
                case ScrollEventType.ThumbTrack:
                    // to avoid performance problems use a refresh delay implemented with a timer
                    if (_thumbTrackTimer.Enabled) // stop old timer
                        _thumbTrackTimer.Enabled = false;
                    // perform scroll immediately only if last refresh is very old
                    var currentThumbTrack = Environment.TickCount;
                    if (currentThumbTrack - _lastThumbtrack > THUMPTRACKDELAY)
                    {
                        PerformScrollThumbTrack(null, null);
                        _lastThumbtrack = currentThumbTrack;
                        break;
                    }
                    // start thumbtrack timer 
                    _thumbTrackPosition = FromScrollPos(e.NewValue);
                    _thumbTrackTimer.Enabled = true;
                    break;
                case ScrollEventType.First:
                    break;
                default:
                    break;
            }
            e.NewValue = ToScrollPos(_scrollVpos);
        }

        /// <summary>
        ///   Performs the thumbtrack scrolling after an delay.
        /// </summary>
        private void PerformScrollThumbTrack(object sender, EventArgs e)
        {
            _thumbTrackTimer.Enabled = false;
            PerformScrollThumpPosition(_thumbTrackPosition);
            _lastThumbtrack = Environment.TickCount;
        }

        private void UpdateScrollSize()
        {
            Debug.WriteLine("UpdateScrollSize()", "HexBox");
            // calc scroll bar info
            if (VScrollBarVisible && _byteProvider != null && _byteProvider.Length > 0 && HorizontalByteCount != 0)
            {
                var scrollmax =
                    (long)
                    Math.Ceiling((_byteProvider.Length + 1) / (double)HorizontalByteCount - VerticalByteCount);
                scrollmax = Math.Max(0, scrollmax);
                var scrollpos = _startByte / HorizontalByteCount;
                if (scrollmax < _scrollVmax) /* Data size has been decreased. */
                    if (_scrollVpos == _scrollVmax)
                        /* Scroll one line up if we at bottom. */
                        PerformScrollLineUp();
                if (scrollmax == _scrollVmax && scrollpos == _scrollVpos)
                    return;
                _scrollVmin = 0;
                _scrollVmax = scrollmax;
                _scrollVpos = Math.Min(scrollpos, scrollmax);
                UpdateVScroll();
            }
            else if (VScrollBarVisible)
            {
                // disable scroll bar
                _scrollVmin = 0;
                _scrollVmax = 0;
                _scrollVpos = 0;
                UpdateVScroll();
            }
        }

        private void UpdateVScroll()
        {
            Debug.WriteLine("UpdateVScroll()", "HexBox");
            var max = ToScrollMax(_scrollVmax);
            if (max > 0)
            {
                _vScrollBar.Minimum = 0;
                _vScrollBar.Maximum = max;
                _vScrollBar.Value = ToScrollPos(_scrollVpos);
                _vScrollBar.Enabled = true;
            }
            else _vScrollBar.Enabled = false;
        }

        private int ToScrollPos(long value)
        {
            if (_scrollVmax < int.MaxValue)
                return (int)value;
            var valperc = (double)value / _scrollVmax * 100;
            var res = (int)Math.Floor((double)int.MaxValue / 100 * valperc); //TODO ...
            res = (int)Math.Max(_scrollVmin, res);
            res = (int)Math.Min(_scrollVmax, res);
            return res;
        }

        private long FromScrollPos(int value)
        {
            if (_scrollVmax < int.MaxValue) return value;
            var valperc = (double)value / int.MaxValue * 100;
            return (long)Math.Floor((double)_scrollVmax / 100 * valperc);
        }

        private static int ToScrollMax(long value)
        {
            return Math.Min((int)value, int.MaxValue);
        }

        private void PerformScrollToLine(long pos)
        {
            if (pos < _scrollVmin || pos > _scrollVmax || pos == _scrollVpos)
                return;
            _scrollVpos = pos;
            UpdateVScroll();
            UpdateVisibilityBytes();
            UpdateCaret();
            Invalidate();
        }

        private void PerformScrollLines(int lines)
        {
            long pos;
            if (lines > 0) pos = Math.Min(_scrollVmax, _scrollVpos + lines);
            else if (lines < 0) pos = Math.Max(_scrollVmin, _scrollVpos + lines);
            else return;
            PerformScrollToLine(pos);
        }

        private void PerformScrollLineDown()
        {
            PerformScrollLines(1);
        }

        private void PerformScrollLineUp()
        {
            PerformScrollLines(-1);
        }

        private void PerformScrollPageDown()
        {
            PerformScrollLines(VerticalByteCount);
        }

        private void PerformScrollPageUp()
        {
            PerformScrollLines(-VerticalByteCount);
        }

        private void PerformScrollThumpPosition(long pos)
        {
            // Bug fix: Scroll to end, do not scroll to end
            var difference = (_scrollVmax > 65535) ? 10 : 9;
            if (ToScrollPos(pos) == ToScrollMax(_scrollVmax) - difference)
                pos = _scrollVmax;
            // End Bug fix
            PerformScrollToLine(pos);
        }

        /// <summary>
        ///   Scrolls the selection start byte into view
        /// </summary>
        public void ScrollByteIntoView()
        {
            Debug.WriteLine("ScrollByteIntoView()", "HexBox");
            ScrollByteIntoView(_bytePos);
        }

        /// <summary>
        ///   Scrolls the specific byte into view
        /// </summary>
        /// <param name = "index">the index of the byte</param>
        public void ScrollByteIntoView(long index)
        {
            Debug.WriteLine("ScrollByteIntoView(long index)", "HexBox");
            if (_byteProvider == null || _keyInterpreter == null)
                return;
            if (index < _startByte)
            {
                var line = (long)Math.Floor(index / (double)HorizontalByteCount);
                PerformScrollThumpPosition(line);
            }
            else if (index > _endByte)
            {
                var line = (long)Math.Floor(index / (double)HorizontalByteCount);
                line -= VerticalByteCount - 1;
                PerformScrollThumpPosition(line);
            }
        }
        #endregion

        #region Selection methods
        private void ReleaseSelection()
        {
            Debug.WriteLine("ReleaseSelection()", "HexBox");
            if (_selectionLength == 0)
                return;
            _selectionLength = 0;
            OnSelectionLengthChanged(EventArgs.Empty);
            if (!_caretVisible)
                CreateCaret();
            else
                UpdateCaret();
            Invalidate();
        }

        /// <summary>
        ///   Returns true if Select method could be invoked.
        /// </summary>
        public new bool CanSelect()
        {
            return _byteProvider != null && Enabled;
        }

        /// <summary>
        ///   Selects all bytes.
        /// </summary>
        public void SelectAll()
        {
            if (ByteProvider == null)
                return;
            Select(0, ByteProvider.Length);
        }

        /// <summary>
        ///   Selects the hex box.
        /// </summary>
        /// <param name = "start">the start index of the selection</param>
        /// <param name = "length">the length of the selection</param>
        public void Select(long start, long length)
        {
            if (ByteProvider == null)
                return;
            if (!Enabled)
                return;
            InternalSelect(start, length);
            ScrollByteIntoView();
        }

        private void InternalSelect(long start, long length)
        {
            if (length > 0 && _caretVisible)
                DestroyCaret();
            else if (length == 0 && !_caretVisible)
                CreateCaret();
            SetPosition(start, 0);
            SetSelectionLength(length);
            UpdateCaret();
            Invalidate();
        }
        #endregion

        #region Key interpreter methods
        private void ActivateEmptyKeyInterpreter()
        {
            if (_eki == null)
                _eki = new EmptyKeyInterpreter(this);
            if (_eki == _keyInterpreter)
                return;
            if (_keyInterpreter != null)
                _keyInterpreter.Deactivate();
            _keyInterpreter = _eki;
            _keyInterpreter.Activate();
        }

        private void ActivateKeyInterpreter()
        {
            if (_ki == null)
                _ki = new KeyInterpreter(this);
            if (_ki == _keyInterpreter)
                return;
            if (_keyInterpreter != null)
                _keyInterpreter.Deactivate();
            _keyInterpreter = _ki;
            _keyInterpreter.Activate();
        }

        private void ActivateStringKeyInterpreter()
        {
            if (_ski == null)
                _ski = new StringKeyInterpreter(this);
            if (_ski == _keyInterpreter)
                return;
            if (_keyInterpreter != null)
                _keyInterpreter.Deactivate();
            _keyInterpreter = _ski;
            _keyInterpreter.Activate();
        }
        #endregion

        #region Caret methods
        private void CreateCaret()
        {
            if (_byteProvider == null || _keyInterpreter == null || _caretVisible || !Focused)
                return;
            Debug.WriteLine("CreateCaret()", "HexBox");
            // define the caret width depending on InsertActive mode
            var caretWidth = (InsertActive) ? 1 : (int)_charSize.Width;
            var caretHeight = (int)_charSize.Height;
            NativeMethods.CreateCaret(Handle, IntPtr.Zero, caretWidth, caretHeight);
            UpdateCaret();
            NativeMethods.ShowCaret(Handle);
            _caretVisible = true;
        }

        private void UpdateCaret()
        {
            if (_byteProvider == null || _keyInterpreter == null)
                return;
            Debug.WriteLine("UpdateCaret()", "HexBox");
            var byteIndex = _bytePos - _startByte;
            var p = _keyInterpreter.GetCaretPointF(byteIndex);
            p.X += _byteCharacterPos * _charSize.Width;
            NativeMethods.SetCaretPos((int)p.X, (int)p.Y);
        }

        private void DestroyCaret()
        {
            if (!_caretVisible)
                return;
            Debug.WriteLine("DestroyCaret()", "HexBox");
            NativeMethods.DestroyCaret();
            _caretVisible = false;
        }

        private void SetCaretPosition(Point p)
        {
            Debug.WriteLine("SetCaretPosition()", "HexBox");
            if (_byteProvider == null || _keyInterpreter == null)
                return;
            long pos;
            int cp;
            if (_recHex.Contains(p))
            {
                var bpi = GetHexBytePositionInfo(p);
                pos = bpi.Index;
                cp = bpi.CharacterPosition;
                SetPosition(pos, cp);
                ActivateKeyInterpreter();
                UpdateCaret();
                Invalidate();
            }
            else if (_recStringView.Contains(p))
            {
                var bpi = GetStringBytePositionInfo(p);
                pos = bpi.Index;
                cp = bpi.CharacterPosition;
                SetPosition(pos, cp);
                ActivateStringKeyInterpreter();
                UpdateCaret();
                Invalidate();
            }
        }

        private BytePositionInfo GetHexBytePositionInfo(Point p)
        {
            Debug.WriteLine("GetHexBytePositionInfo()", "HexBox");
            var x = (int)((p.X - _recHex.X) / _charSize.Width);
            var y = (int)((p.Y - _recHex.Y) / _charSize.Height);
            var hPos = (x / 2); //TODO MOUSE HERE
            if (hPos > 0 && (hPos + 1) % 5 == 0) //If you click in the space
            {
                hPos++; //Select Next Byte
                x = 0; //Select the 1st digit
            }
            hPos = hPos - hPos / 5; //Adjust for the spacing
            var bytePos = Math.Min(_byteProvider.Length,
                                   _startByte + (HorizontalByteCount * (y + 1) - HorizontalByteCount) + hPos);
            var byteCharacterPos = (x % 2);
            if (bytePos == _byteProvider.Length)
                byteCharacterPos = 0;
            return bytePos < 0
                       ? new BytePositionInfo(0, 0)
                       : new BytePositionInfo(bytePos, byteCharacterPos);
        }

        private BytePositionInfo GetStringBytePositionInfo(Point p)
        {
            Debug.WriteLine("GetStringBytePositionInfo()", "HexBox");
            var x = (int)((p.X - _recStringView.X) / _charSize.Width);
            var y = (int)((p.Y - _recStringView.Y) / _charSize.Height);
            var bytePos = Math.Min(_byteProvider.Length,
                                   _startByte + (HorizontalByteCount * (y + 1) - HorizontalByteCount) + x);
            return new BytePositionInfo(Math.Min(bytePos, 0), 0);
        }
        #endregion

        #region PreProcessMessage methods
        /// <summary>
        ///   Preprocesses windows messages.
        /// </summary>
        /// <param name = "m">the message to process.</param>
        /// <returns>true, if the message was processed</returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true),
         SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        public override bool PreProcessMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WMKeydown:
                    return _keyInterpreter.PreProcessWmKeyDown(ref m);
                case NativeMethods.WMChar:
                    return _keyInterpreter.PreProcessWmChar(ref m);
                case NativeMethods.WMKeyup:
                    return _keyInterpreter.PreProcessWmKeyUp(ref m);
                default:
                    return base.PreProcessMessage(ref m);
            }
        }

        private bool BasePreProcessMessage(ref Message m)
        {
            return base.PreProcessMessage(ref m);
        }
        #endregion

        #region Find methods
        /// <summary>
        ///   Searches the current ByteProvider
        /// </summary>
        /// <param name = "bytes">the array of bytes to find</param>
        /// <param name = "startIndex">the start index</param>
        /// <returns>the SelectionStart property value if find was successfull or
        ///   -1 if there is no match
        ///   -2 if Find was aborted.</returns>
        public long Find(byte[] bytes, long startIndex)
        {
            var match = 0;
            var bytesLength = bytes.Length;
            _abortFind = false;
            for (var pos = startIndex; pos < _byteProvider.Length; pos++)
            {
                if (_abortFind)
                    return -2;
                if (pos % 1000 == 0) // for performance reasons: DoEvents only 1 times per 1000 loops
                    Application.DoEvents();
                if (_byteProvider.ReadByte(pos) != bytes[match])
                {
                    pos -= match;
                    match = 0;
                    CurrentFindingPosition = pos;
                    continue;
                }
                match++;
                if (match != bytesLength) continue;
                var bytePos = pos - bytesLength + 1;
                Select(bytePos, bytesLength);
                ScrollByteIntoView(_bytePos + _selectionLength);
                ScrollByteIntoView(_bytePos);
                return bytePos;
            }
            return -1;
        }

        /// <summary>
        ///   Aborts a working Find method.
        /// </summary>
        public void AbortFind()
        {
            _abortFind = true;
        }

        /// <summary>
        ///   Gets a value that indicates the current position during Find method execution.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long CurrentFindingPosition { get; private set; }
        #endregion

        #region Copy, Cut and Paste methods
        private byte[] GetCopyData()
        {
            if (!CanCopy()) return new byte[0];
            // put bytes into buffer
            var buffer = new byte[_selectionLength];
            var id = -1;
            for (var i = _bytePos; i < _bytePos + _selectionLength; i++)
            {
                id++;
                buffer[id] = _byteProvider.ReadByte(i);
            }
            return buffer;
        }

        /// <summary>
        ///   Copies the current selection in the hex box to the Clipboard.
        /// </summary>
        public void Copy()
        {
            if (!CanCopy()) return;
            // put bytes into buffer
            var buffer = GetCopyData();
            var da = new DataObject();
            // set string buffer clipbard data
            var sBuffer = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            da.SetData(typeof(string), sBuffer);
            //set memorystream (BinaryData) clipboard data
            var ms = new MemoryStream(buffer, 0, buffer.Length, false, true);
            da.SetData("BinaryData", ms);
            Clipboard.SetDataObject(da, true);
            UpdateCaret();
            ScrollByteIntoView();
            Invalidate();
            OnCopied(EventArgs.Empty);
        }

        /// <summary>
        ///   Return true if Copy method could be invoked.
        /// </summary>
        public bool CanCopy()
        {
            return _selectionLength >= 1 && _byteProvider != null;
        }

        /// <summary>
        ///   Moves the current selection in the hex box to the Clipboard.
        /// </summary>
        public void Cut()
        {
            if (!CanCut()) return;
            Copy();
            _byteProvider.DeleteBytes(_bytePos, _selectionLength);
            _byteCharacterPos = 0;
            UpdateCaret();
            ScrollByteIntoView();
            ReleaseSelection();
            Invalidate();
            Refresh();
        }

        /// <summary>
        ///   Return true if Cut method could be invoked.
        /// </summary>
        public bool CanCut()
        {
            if (ReadOnly || !Enabled)
                return false;
            if (_byteProvider == null)
                return false;
            return _selectionLength >= 1 && _byteProvider.SupportsDeleteBytes();
        }

        /// <summary>
        ///   Replaces the current selection in the hex box with the contents of the Clipboard.
        /// </summary>
        public void Paste()
        {
            if (!CanPaste()) return;
            if (_selectionLength > 0)
                _byteProvider.DeleteBytes(_bytePos, _selectionLength);
            byte[] buffer;
            var da = Clipboard.GetDataObject();
            Debug.Assert(da != null);
            if (da.GetDataPresent("BinaryData"))
            {
                var ms = (MemoryStream)da.GetData("BinaryData");
                buffer = new byte[ms.Length];
                ms.Read(buffer, 0, buffer.Length);
            }
            else if (da.GetDataPresent(typeof(string)))
            {
                var sBuffer = (string)da.GetData(typeof(string));
                buffer = Encoding.ASCII.GetBytes(sBuffer);
            }
            else return;
            _byteProvider.InsertBytes(_bytePos, buffer);
            SetPosition(_bytePos + buffer.Length, 0);
            ReleaseSelection();
            ScrollByteIntoView();
            UpdateCaret();
            Invalidate();
        }

        /// <summary>
        ///   Return true if Paste method could be invoked.
        /// </summary>
        public bool CanPaste()
        {
            if (ReadOnly || !Enabled) return false;
            if (_byteProvider == null || !_byteProvider.SupportsInsertBytes())
                return false;
            if (!_byteProvider.SupportsDeleteBytes() && _selectionLength > 0)
                return false;
            var da = Clipboard.GetDataObject();
            Debug.Assert(da != null);
            return da.GetDataPresent("BinaryData") || da.GetDataPresent(typeof(string));
        }

        /// <summary>
        ///   Return true if PasteHex method could be invoked.
        /// </summary>
        public bool CanPasteHex()
        {
            if (!CanPaste()) return false;
            var da = Clipboard.GetDataObject();
            Debug.Assert(da != null);
            if (da.GetDataPresent(typeof(string)))
            {
                var hexString = (string)da.GetData(typeof(string));
                var buffer = ConvertHexToBytes(hexString);
                return (buffer != null);
            }
            return false;
        }

        /// <summary>
        ///   Replaces the current selection in the hex box with the hex string data of the Clipboard.
        /// </summary>
        public void PasteHex()
        {
            if (!CanPaste()) return;
            byte[] buffer;
            var da = Clipboard.GetDataObject();
            Debug.Assert(da != null);
            if (da.GetDataPresent(typeof(string)))
            {
                var hexString = (string)da.GetData(typeof(string));
                buffer = ConvertHexToBytes(hexString);
                if (buffer == null)
                    return;
            }
            else return;
            if (_selectionLength > 0)
                _byteProvider.DeleteBytes(_bytePos, _selectionLength);
            _byteProvider.InsertBytes(_bytePos, buffer);
            SetPosition(_bytePos + buffer.Length, 0);
            ReleaseSelection();
            ScrollByteIntoView();
            UpdateCaret();
            Invalidate();
        }

        /// <summary>
        ///   Copies the current selection in the hex box to the Clipboard in hex format.
        /// </summary>
        public void CopyHex()
        {
            if (!CanCopy()) return;
            // put bytes into buffer
            var buffer = GetCopyData();
            var da = new DataObject();
            // set string buffer clipbard data
            var hexString = ConvertBytesToHex(buffer);
            da.SetData(typeof(string), hexString);
            //set memorystream (BinaryData) clipboard data
            var ms = new MemoryStream(buffer, 0, buffer.Length, false, true);
            da.SetData("BinaryData", ms);
            Clipboard.SetDataObject(da, true);
            UpdateCaret();
            ScrollByteIntoView();
            Invalidate();
            OnCopiedHex(EventArgs.Empty);
        }
        #endregion

        #region Paint methods
        /// <summary>
        ///   Paints the background.
        /// </summary>
        /// <param name = "e">A PaintEventArgs that contains the event data.</param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            switch (_borderStyle)
            {
                case BorderStyle.Fixed3D:
                    {
                        if (TextBoxRenderer.IsSupported)
                        {
                            var state = VisualStyleElement.TextBox.TextEdit.Normal;
                            var backColor = BackColor;
                            if (Enabled)
                            {
                                if (ReadOnly)
                                    state = VisualStyleElement.TextBox.TextEdit.ReadOnly;
                                else if (Focused)
                                    state = VisualStyleElement.TextBox.TextEdit.Focused;
                            }
                            else
                            {
                                state = VisualStyleElement.TextBox.TextEdit.Disabled;
                                backColor = BackColorDisabled;
                            }
                            var vsr = new VisualStyleRenderer(state);
                            vsr.DrawBackground(e.Graphics, ClientRectangle);
                            var rectContent = vsr.GetBackgroundContentRectangle(e.Graphics, ClientRectangle);
                            e.Graphics.FillRectangle(new SolidBrush(backColor), rectContent);
                        }
                        else
                        {
                            // draw background
                            e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
                            // draw default border
                            ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Sunken);
                        }
                        break;
                    }
                case BorderStyle.FixedSingle:
                    {
                        // draw background
                        e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
                        // draw fixed single border
                        ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.Black, ButtonBorderStyle.Solid);
                        break;
                    }
            }
        }

        /// <summary>
        ///   Paints the hex box.
        /// </summary>
        /// <param name = "e">A PaintEventArgs that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_byteProvider == null)
                return;
            Debug.WriteLine("OnPaint " + DateTime.Now, "HexBox");
            // draw only in the content rectangle, so exclude the border and the scrollbar.
            var r = new Region(ClientRectangle);
            r.Exclude(_recContent);
            e.Graphics.ExcludeClip(r);
            UpdateVisibilityBytes();
            if (_lineInfoVisible)
                PaintLineInfo(e.Graphics, _startByte, _endByte);
            if (!_stringViewVisible)
                PaintHex(e.Graphics, _startByte, _endByte);
            else
            {
                PaintHexAndStringView(e.Graphics, _startByte, _endByte);
                if (_shadowSelectionVisible)
                    PaintCurrentBytesSign(e.Graphics);
                PaintHeaderRow(e.Graphics);
            }
        }

        private void PaintHighlight(Graphics g, HexboxHighlight h)
        {
                var byteGridPoint = GetGridBytePoint(h._startIndex, true);
                var bytePointF = GetBytePointF(byteGridPoint);
                g.FillRectangle(new SolidBrush(h._color), bytePointF.X, bytePointF.Y, _charSize.Width * (h._length * 4) - 9,
                            _charSize.Height);
                g.DrawRectangle(new Pen(new SolidBrush(Color.Black)), bytePointF.X, bytePointF.Y, _charSize.Width * (h._length * 4),
                            _charSize.Height);
        }

        private void PaintLineInfo(Graphics g, long startByte, long endByte)
        {
            // Ensure endByte isn't > length of array.
            endByte = Math.Min(_byteProvider.Length - 1, endByte);
            var brush = SystemBrushes.ControlText;
            var maxLine = GetGridBytePoint(endByte - startByte, true).Y + 1;
            g.FillRectangle(SystemBrushes.ControlLight, new Rectangle(0, 0, 80, Height));
            g.DrawRectangle(SystemPens.ControlDark, new Rectangle(0, 0, 80, Height));
            for (var i = 0; i < maxLine; i++)
            {
                var firstLineByte = startByte + (HorizontalByteCount) * i;
                var bytePointF = GetBytePointF(new Point(0, 0 + i));
                var info = firstLineByte.ToString(_hexStringFormat,
                                                  Thread.CurrentThread.CurrentCulture);
                var nulls = 8 - info.Length;
                string formattedInfo;
                if (nulls > -1) formattedInfo = new string('0', 8 - info.Length) + info;
                else formattedInfo = new string('~', 8);
                g.DrawString(formattedInfo, Font, brush, new PointF(_recLineInfo.X, bytePointF.Y), _stringFormat);
            }
        }

        private void PaintHex(Graphics g, long startByte, long endByte)
        {
            Brush brush = new SolidBrush(GetDefaultForeColor());
            Brush selBrush = new SolidBrush(_selectionForeColor);
            Brush selBrushBack = new SolidBrush(_selectionBackColor);
            var counter = -1;
            var internalEndByte = Math.Min(_byteProvider.Length - 1, endByte + HorizontalByteCount);
            var isKeyInterpreterActive = _keyInterpreter == null ||
                                         _keyInterpreter.GetType() == typeof(KeyInterpreter);
            for (var i = startByte; i < internalEndByte + 1; i++)
            {
                counter++;
                var gridPoint = GetGridBytePoint(counter, true);
                var b = _byteProvider.ReadByte(i);
                var isSelectedByte = i >= _bytePos && i <= (_bytePos + _selectionLength - 1) && _selectionLength != 0;
                if (isSelectedByte && isKeyInterpreterActive)
                    PaintHexStringSelected(g, b, selBrush, selBrushBack, gridPoint);
                else
                {
                    var pf = GetBytePointF(gridPoint);
                    g.FillRectangle(new SolidBrush(_byteProvider.GetByteColor(i)), pf.X, pf.Y, (int)_charSize.Width * 2,
                                    (int)_charSize.Height);
                    PaintHexString(g, b, brush, gridPoint);
                }
            }
        }

        private void PaintHexString(Graphics g, byte b, Brush brush, Point gridPoint)
        {
            var bytePointF = GetBytePointF(gridPoint);
            var sB = ConvertByteToHex(b);
            g.DrawString(sB.Substring(0, 1), Font, brush, bytePointF, _stringFormat);
            bytePointF.X += _charSize.Width;
            g.DrawString(sB.Substring(1, 1), Font, brush, bytePointF, _stringFormat);
        }

        private void PaintHexStringSelected(Graphics g, byte b, Brush brush, Brush brushBack, Point gridPoint)
        {
            //TODO
            var sB = b.ToString(_hexStringFormat, Thread.CurrentThread.CurrentCulture);
            if (sB.Length == 1)
                sB = "0" + sB;
            var bytePointF = GetBytePointF(gridPoint);
            const bool isLastLineChar = true; // (gridPoint.X + 1 == _iHexMaxHBytes);
            var bcWidth = (isLastLineChar) ? _charSize.Width * 2 : _charSize.Width * 3;
            g.FillRectangle(brushBack, bytePointF.X, bytePointF.Y, bcWidth, _charSize.Height);
            g.DrawString(sB.Substring(0, 1), Font, brush, bytePointF, _stringFormat);
            bytePointF.X += _charSize.Width;
            g.DrawString(sB.Substring(1, 1), Font, brush, bytePointF, _stringFormat);
        }

        private void PaintHexAndStringView(Graphics g, long startByte, long endByte)
        {
            Brush brush = new SolidBrush(GetDefaultForeColor());
            Brush selBrush = new SolidBrush(_selectionForeColor);
            Brush selBrushBack = new SolidBrush(_selectionBackColor);

            var counter = -1;
            var internalEndByte = Math.Min(_byteProvider.Length - 1, endByte + HorizontalByteCount);
            var isKeyInterpreterActive = _keyInterpreter == null ||
                                         _keyInterpreter.GetType() == typeof(KeyInterpreter);
            var isStringKeyInterpreterActive = _keyInterpreter != null &&
                                               _keyInterpreter.GetType() == typeof(StringKeyInterpreter);
            for (var i = startByte; i < internalEndByte + 1; i++)
            {
                counter++;

                var StringGridPoint = GetGridBytePoint(counter, false);
                var HexGridPoint = GetGridBytePoint(counter, true);
                var HexBytePointF = GetBytePointF(HexGridPoint);
                var byteStringPointF = GetByteStringPointF(StringGridPoint);
                var b = _byteProvider.ReadByte(i);

                var isSelectedByte = i >= _bytePos && i <= (_bytePos + _selectionLength - 1) && _selectionLength != 0;

                if (isSelectedByte && isKeyInterpreterActive)
                { PaintHexStringSelected(g, b, selBrush, selBrushBack, HexGridPoint); }
                else
                { PaintHexString(g, b, brush, HexGridPoint); }

                string s;
                if (b > 0x1F && !(b > 0x7E && b < 0xA0)) s = ((char)b).ToString();
                else s = ".";
                if (isSelectedByte && isStringKeyInterpreterActive)
                {
                    g.FillRectangle(selBrushBack, byteStringPointF.X, byteStringPointF.Y, _charSize.Width,
                                    _charSize.Height);
                    g.DrawString(s, Font, selBrush, byteStringPointF, _stringFormat);
                }
                else
                    g.DrawString(s, Font, brush, byteStringPointF, _stringFormat);
            }
        }

        void PaintHeaderRow(Graphics g)
        {
            Brush brush = new SolidBrush(Color.Black);
            g.FillRectangle(SystemBrushes.ControlLight, new Rectangle(0, 0, _recContent.Width, (int)_charSize.Height));
            g.DrawRectangle(SystemPens.ControlDark, new Rectangle(0, 0, _recContent.Width, (int)_charSize.Height));

            for (int col = 0; col < 16; col += 4)
            {
                PaintColumnInfo(g, (byte)col, brush, col);
            }
        }

        PointF GetColumnInfoPointF(int col)
        {
            Point gp = GetGridBytePoint(col, false);
            float x = (2.5f * _charSize.Width) * gp.X + _recColumnInfo.X;
            float y = _recColumnInfo.Y;

            return new PointF(x, y);
        }

        void PaintColumnInfo(Graphics g, byte b, Brush brush, int col)
        {
            PointF headerPointF = GetColumnInfoPointF(col);

            string sB = ConvertByteToHex(b);
            g.DrawString(sB.Substring(0, 1), Font, brush, headerPointF, _stringFormat);
            headerPointF.X += _charSize.Width;
            g.DrawString(sB.Substring(1, 1), Font, brush, headerPointF, _stringFormat);
        }

        private void PaintCurrentBytesSign(Graphics g)
        {
            if (_keyInterpreter != null && Focused && _bytePos != -1 && Enabled)
                if (_keyInterpreter.GetType() == typeof(KeyInterpreter))
                    if (_selectionLength == 0)
                    {
                        var gp = GetGridBytePoint(_bytePos - _startByte, true);
                        var pf = GetByteStringPointF(gp);
                        var s = new Size((int)_charSize.Width, (int)_charSize.Height);
                        var r = new Rectangle((int)pf.X, (int)pf.Y, s.Width, s.Height);
                        if (r.IntersectsWith(_recStringView))
                        {
                            r.Intersect(_recStringView);
                            PaintCurrentByteSign(g, r);
                        }
                    }
                    else
                    {
                        var lineWidth = (int)(_recStringView.Width - _charSize.Width);
                        var startSelGridPoint = GetGridBytePoint(_bytePos - _startByte, true);
                        var startSelPointF = GetByteStringPointF(startSelGridPoint);
                        var endSelGridPoint = GetGridBytePoint(_bytePos - _startByte + _selectionLength - 1, true);
                        var endSelPointF = GetByteStringPointF(endSelGridPoint);
                        var multiLine = endSelGridPoint.Y - startSelGridPoint.Y;
                        if (multiLine == 0)
                        {
                            var singleLine = new Rectangle(
                                (int)startSelPointF.X,
                                (int)startSelPointF.Y,
                                (int)(endSelPointF.X - startSelPointF.X + _charSize.Width),
                                (int)_charSize.Height);
                            if (singleLine.IntersectsWith(_recStringView))
                            {
                                singleLine.Intersect(_recStringView);
                                PaintCurrentByteSign(g, singleLine);
                            }
                        }
                        else
                        {
                            var firstLine = new Rectangle(
                                (int)startSelPointF.X,
                                (int)startSelPointF.Y,
                                (int)(_recStringView.X + lineWidth - startSelPointF.X + _charSize.Width),
                                (int)_charSize.Height);
                            if (firstLine.IntersectsWith(_recStringView))
                            {
                                firstLine.Intersect(_recStringView);
                                PaintCurrentByteSign(g, firstLine);
                            }
                            if (multiLine > 1)
                            {
                                var betweenLines = new Rectangle(
                                    _recStringView.X,
                                    (int)(startSelPointF.Y + _charSize.Height),
                                    (_recStringView.Width),
                                    (int)(_charSize.Height * (multiLine - 1)));
                                if (betweenLines.IntersectsWith(_recStringView))
                                {
                                    betweenLines.Intersect(_recStringView);
                                    PaintCurrentByteSign(g, betweenLines);
                                }
                            }
                            var lastLine = new Rectangle(
                                _recStringView.X,
                                (int)endSelPointF.Y,
                                (int)(endSelPointF.X - _recStringView.X + _charSize.Width),
                                (int)_charSize.Height);
                            if (lastLine.IntersectsWith(_recStringView))
                            {
                                lastLine.Intersect(_recStringView);
                                PaintCurrentByteSign(g, lastLine);
                            }
                        }
                    }
                else if (_selectionLength == 0)
                {
                    var gp = GetGridBytePoint(_bytePos - _startByte, true);
                    var pf = GetBytePointF(gp);
                    var s = new Size((int)_charSize.Width * 2, (int)_charSize.Height);
                    var r = new Rectangle((int)pf.X, (int)pf.Y, s.Width, s.Height);
                    PaintCurrentByteSign(g, r);
                }
                else
                {
                    var lineWidth = (int)(_recHex.Width - _charSize.Width * 5);
                    var startSelGridPoint = GetGridBytePoint(_bytePos - _startByte, true);
                    var startSelPointF = GetBytePointF(startSelGridPoint);
                    var endSelGridPoint = GetGridBytePoint(_bytePos - _startByte + _selectionLength - 1, true);
                    var endSelPointF = GetBytePointF(endSelGridPoint);
                    var multiLine = endSelGridPoint.Y - startSelGridPoint.Y;
                    if (multiLine == 0)
                    {
                        var singleLine = new Rectangle(
                            (int)startSelPointF.X,
                            (int)startSelPointF.Y,
                            (int)(endSelPointF.X - startSelPointF.X + _charSize.Width * 2),
                            (int)_charSize.Height);
                        if (singleLine.IntersectsWith(_recHex))
                        {
                            singleLine.Intersect(_recHex);
                            PaintCurrentByteSign(g, singleLine);
                        }
                    }
                    else
                    {
                        var firstLine = new Rectangle(
                            (int)startSelPointF.X,
                            (int)startSelPointF.Y,
                            (int)(_recHex.X + lineWidth - startSelPointF.X + _charSize.Width * 2),
                            (int)_charSize.Height);
                        if (firstLine.IntersectsWith(_recHex))
                        {
                            firstLine.Intersect(_recHex);
                            PaintCurrentByteSign(g, firstLine);
                        }
                        if (multiLine > 1)
                        {
                            var betweenLines = new Rectangle(
                                _recHex.X,
                                (int)(startSelPointF.Y + _charSize.Height),
                                (int)(lineWidth + _charSize.Width * 2),
                                (int)(_charSize.Height * (multiLine - 1)));
                            if (betweenLines.IntersectsWith(_recHex))
                            {
                                betweenLines.Intersect(_recHex);
                                PaintCurrentByteSign(g, betweenLines);
                            }
                        }
                        var lastLine = new Rectangle(
                            _recHex.X,
                            (int)endSelPointF.Y,
                            (int)(endSelPointF.X - _recHex.X + _charSize.Width * 2),
                            (int)_charSize.Height);
                        if (lastLine.IntersectsWith(_recHex))
                        {
                            lastLine.Intersect(_recHex);
                            PaintCurrentByteSign(g, lastLine);
                        }
                    }
                }
        }

        private void PaintCurrentByteSign(Graphics g, Rectangle rec)
        {
            // stack overflowexception on big files - workaround
            if (rec.Top < 0 || rec.Left < 0 || rec.Width <= 0 || rec.Height <= 0)
                return;
            var myBitmap = new Bitmap(rec.Width, rec.Height);
            var bitmapGraphics = Graphics.FromImage(myBitmap);
            var greenBrush = new SolidBrush(_shadowSelectionColor);
            bitmapGraphics.FillRectangle(greenBrush, 0,
                                         0, rec.Width, rec.Height);
            g.CompositingQuality = CompositingQuality.GammaCorrected;
            g.DrawImage(myBitmap, rec.Left, rec.Top);
        }

        private Color GetDefaultForeColor()
        {
            return Enabled ? ForeColor : Color.Gray;
        }

        private void UpdateVisibilityBytes()
        {
            if (_byteProvider == null || _byteProvider.Length == 0)
                return;
            _startByte = (_scrollVpos + 1) * HorizontalByteCount - HorizontalByteCount;
            _endByte = Math.Min(_byteProvider.Length - 1, _startByte + _iHexMaxBytes);
        }
        #endregion

        #region Positioning methods
        private void UpdateRectanglePositioning()
        {
            // calc char size
            var charSize = CreateGraphics().MeasureString("A", Font, 100, _stringFormat);
            _charSize = new SizeF((float)Math.Ceiling(charSize.Width), (float)Math.Ceiling(charSize.Height));
            // calc content bounds
            _recContent = ClientRectangle;
            _recContent.X += _recBorderLeft;
            _recContent.Y += _recBorderTop;
            _recContent.Width -= _recBorderRight + _recBorderLeft;
            _recContent.Height -= _recBorderBottom + _recBorderTop;
            if (_vScrollBarVisible)
            {
                _recContent.Width -= _vScrollBar.Width;
                _vScrollBar.Left = _recContent.X + _recContent.Width;
                _vScrollBar.Top = _recContent.Y;
                _vScrollBar.Height = _recContent.Height;
            }
            const int marginLeft = 4; //TODO
            // calc line info bounds
            _recColumnInfo = new Rectangle(_recLineInfo.X + _recLineInfo.Width, _recContent.Y, _recContent.Width - _recLineInfo.Width, (int)charSize.Height + 4);
            if (_lineInfoVisible)
                _recLineInfo = new Rectangle(_recContent.X + marginLeft,
                                             _recContent.Y,
                                             (int)(_charSize.Width * 10),
                                             _recContent.Height - _recColumnInfo.Height);
            else
            {
                _recLineInfo = Rectangle.Empty;
                _recLineInfo.X = marginLeft;
            }

            // calc column info bounds
            _recColumnInfo = new Rectangle(_recLineInfo.X + _recLineInfo.Width, _recContent.Y, _recContent.Width - _recLineInfo.Width, (int)charSize.Height + 4);
            _recLineInfo.Y += (int)charSize.Height + 4;
            _recLineInfo.Height -= (int)charSize.Height + 4;

            // calc hex bounds and grid
            _recHex = new Rectangle(_recLineInfo.X + _recLineInfo.Width,
                                    _recLineInfo.Y,
                                    _recContent.Width - _recLineInfo.Width,
                                    _recContent.Height);
            if (UseFixedBytesPerLine)
            {
                SetHorizontalByteCount(_bytesPerLine);
                _recHex.Width = (int)Math.Floor(((double)HorizontalByteCount) * _charSize.Width * 3 + (2 * _charSize.Width));
            }
            else
            {
                var hmax = (int)Math.Floor(_recHex.Width / (double)_charSize.Width);
                if (hmax > 1)
                    SetHorizontalByteCount((int)Math.Floor((double)hmax / 3));
                else
                    SetHorizontalByteCount(hmax);
            }
            if (_stringViewVisible)
                _recStringView = new Rectangle(_recHex.X + _recHex.Width - 60,
                                               _recHex.Y,
                                               (int)(_charSize.Width * HorizontalByteCount),
                                               _recHex.Height);
            else _recStringView = Rectangle.Empty;
            var vmax = (int)Math.Floor(_recHex.Height / (double)_charSize.Height);
            SetVerticalByteCount(vmax);
            _iHexMaxBytes = HorizontalByteCount * VerticalByteCount;
            UpdateScrollSize();
        }

        private PointF GetBytePointF(long byteIndex)
        {
            var gp = GetGridBytePoint(byteIndex, true);
            return GetBytePointF(gp);
        }

        private PointF GetBytePointF(Point gp)
        {
            var x = (2 * _charSize.Width) * gp.X + _recHex.X; //TODO PAINT HERE
            var y = (gp.Y + 1) * _charSize.Height - _charSize.Height + _recHex.Y;
            return new PointF(x, y);
        }

        private PointF GetByteStringPointF(Point gp)
        {
            var x = (_charSize.Width) * gp.X + _recStringView.X;
            var y = (gp.Y + 1) * _charSize.Height - _charSize.Height + _recStringView.Y;
            return new PointF(x, y);
        }

        private Point GetGridBytePoint(long byteIndex, bool Grouping)
        {
            var row = (int)Math.Floor((double)byteIndex / HorizontalByteCount);
            var column = (int)(byteIndex + HorizontalByteCount - HorizontalByteCount * (row + 1));
            if (Grouping) { column += column / 4; }
            return new Point(column, row);
        }
        #endregion

        #region Overridden properties
        /// <summary>
        ///   Gets or sets the background color for the control.
        /// </summary>
        [DefaultValue(typeof(Color), "White")]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; }
        }

        /// <summary>
        ///   Not used.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         EditorBrowsable(EditorBrowsableState.Never), Bindable(false)]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        /// <summary>
        ///   Not used.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         EditorBrowsable(EditorBrowsableState.Never), Bindable(false)]
        public override RightToLeft RightToLeft
        {
            get { return base.RightToLeft; }
            set { base.RightToLeft = value; }
        }
        #endregion

        #region Properties
        /// <summary>
        ///   Gets or sets the background color for the disabled control.
        /// </summary>
        [Category("Appearance"), DefaultValue(typeof(Color), "WhiteSmoke")]
        public Color BackColorDisabled
        {
            get { return _backColorDisabled; }
            set { _backColorDisabled = value; }
        }

        private Color _backColorDisabled = Color.FromName("WhiteSmoke");

        public List<HexboxHighlight> Highlights
        {
            get { return _highlights; }
            set { _highlights = value; }
        }
        private List<HexboxHighlight> _highlights;

        /// <summary>
        ///   Gets or sets if the count of bytes in one line is fix.
        /// </summary>
        /// <remarks>
        ///   When set to True, BytesPerLine property determine the maximum count of bytes in one line.
        /// </remarks>
        [DefaultValue(false), Category("Hex"), Description("Gets or sets if the count of bytes in one line is fix.")]
        public bool ReadOnly
        {
            get { return _readOnly; }
            set
            {
                if (_readOnly == value)
                    return;
                _readOnly = value;
                OnReadOnlyChanged(EventArgs.Empty);
                Invalidate();
            }
        }

        private bool _readOnly;

        /// <summary>
        ///   Gets or sets the maximum count of bytes in one line.
        /// </summary>
        /// <remarks>
        ///   UsedFixedBytesPerLine property must set to true
        /// </remarks>
        [DefaultValue(16), Category("Hex"), Description("Gets or sets the maximum count of bytes in one line.")]
        public int BytesPerLine
        {
            get { return _bytesPerLine; }
            set
            {
                if (_bytesPerLine == value)
                    return;
                _bytesPerLine = value;
                OnBytesPerLineChanged(EventArgs.Empty);
                UpdateRectanglePositioning();
                Invalidate();
            }
        }

        private int _bytesPerLine = 16;

        /// <summary>
        ///   Gets or sets if the count of bytes in one line is fix.
        /// </summary>
        /// <remarks>
        ///   When set to True, BytesPerLine property determine the maximum count of bytes in one line.
        /// </remarks>
        [DefaultValue(false), Category("Hex"), Description("Gets or sets if the count of bytes in one line is fix.")]
        public bool UseFixedBytesPerLine
        {
            get { return _useFixedBytesPerLine; }
            set
            {
                if (_useFixedBytesPerLine == value)
                    return;
                _useFixedBytesPerLine = value;
                OnUseFixedBytesPerLineChanged(EventArgs.Empty);
                UpdateRectanglePositioning();
                Invalidate();
            }
        }

        private bool _useFixedBytesPerLine;

        /// <summary>
        ///   Gets or sets the visibility of a vertical scroll bar.
        /// </summary>
        [DefaultValue(false), Category("Hex"), Description("Gets or sets the visibility of a vertical scroll bar.")]
        public bool VScrollBarVisible
        {
            get { return _vScrollBarVisible; }
            set
            {
                if (_vScrollBarVisible == value)
                    return;
                _vScrollBarVisible = value;
                if (_vScrollBarVisible)
                    Controls.Add(_vScrollBar);
                else
                    Controls.Remove(_vScrollBar);
                UpdateRectanglePositioning();
                UpdateScrollSize();
                OnVScrollBarVisibleChanged(EventArgs.Empty);
            }
        }

        private bool _vScrollBarVisible;

        /// <summary>
        ///   Gets or sets the ByteProvider.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IByteProvider ByteProvider
        {
            get { return _byteProvider; }
            set
            {
                if (_byteProvider == value)
                    return;
                if (value == null)
                    ActivateEmptyKeyInterpreter();
                else
                    ActivateKeyInterpreter();
                if (_byteProvider != null)
                    _byteProvider.LengthChanged -= ByteProviderLengthChanged;
                _byteProvider = value;
                if (_byteProvider != null)
                    _byteProvider.LengthChanged += ByteProviderLengthChanged;
                OnByteProviderChanged(EventArgs.Empty);
                if (value == null) // do not raise events if value is null
                {
                    _bytePos = -1;
                    _byteCharacterPos = 0;
                    _selectionLength = 0;
                    DestroyCaret();
                }
                else
                {
                    SetPosition(0, 0);
                    SetSelectionLength(0);
                    if (_caretVisible && Focused)
                        UpdateCaret();
                    else
                        CreateCaret();
                }
                CheckCurrentLineChanged();
                CheckCurrentPositionInLineChanged();
                _scrollVpos = 0;
                UpdateVisibilityBytes();
                UpdateRectanglePositioning();
                Invalidate();
            }
        }

        private IByteProvider _byteProvider;

        /// <summary>
        ///   Gets or sets the visibility of a line info.
        /// </summary>
        [DefaultValue(false), Category("Hex"), Description("Gets or sets the visibility of a line info.")]
        public bool LineInfoVisible
        {
            get { return _lineInfoVisible; }
            set
            {
                if (_lineInfoVisible == value)
                    return;
                _lineInfoVisible = value;
                OnLineInfoVisibleChanged(EventArgs.Empty);
                UpdateRectanglePositioning();
                Invalidate();
            }
        }

        private bool _lineInfoVisible;

        /// <summary>
        ///   Gets or sets the hex box큦 border style.
        /// </summary>
        [DefaultValue(typeof(BorderStyle), "Fixed3D"), Category("Hex"),
         Description("Gets or sets the hex box큦 border style.")]
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                if (_borderStyle == value)
                    return;
                _borderStyle = value;
                switch (_borderStyle)
                {
                    case BorderStyle.None:
                        _recBorderLeft = _recBorderTop = _recBorderRight = _recBorderBottom = 0;
                        break;
                    case BorderStyle.Fixed3D:
                        _recBorderLeft = _recBorderRight = SystemInformation.Border3DSize.Width;
                        _recBorderTop = _recBorderBottom = SystemInformation.Border3DSize.Height;
                        break;
                    case BorderStyle.FixedSingle:
                        _recBorderLeft = _recBorderTop = _recBorderRight = _recBorderBottom = 1;
                        break;
                }
                UpdateRectanglePositioning();
                OnBorderStyleChanged(EventArgs.Empty);
            }
        }

        private BorderStyle _borderStyle = BorderStyle.Fixed3D;

        /// <summary>
        ///   Gets or sets the visibility of the string view.
        /// </summary>
        [DefaultValue(false), Category("Hex"), Description("Gets or sets the visibility of the string view.")]
        public bool StringViewVisible
        {
            get { return _stringViewVisible; }
            set
            {
                if (_stringViewVisible == value)
                    return;
                _stringViewVisible = value;
                OnStringViewVisibleChanged(EventArgs.Empty);
                UpdateRectanglePositioning();
                Invalidate();
            }
        }

        private bool _stringViewVisible;

        /// <summary>
        ///   Gets or sets whether the HexBox control displays the hex characters in upper or lower case.
        /// </summary>
        [DefaultValue(typeof(HexCasing), "Upper"), Category("Hex"),
         Description("Gets or sets whether the HexBox control displays the hex characters in upper or lower case.")]
        public HexCasing HexCasing
        {
            get
            {
                return _hexStringFormat == "X" ? HexCasing.Upper : HexCasing.Lower;
            }
            set
            {
                var format = value == HexCasing.Upper ? "X" : "x";
                if (_hexStringFormat == format)
                    return;
                _hexStringFormat = format;
                OnHexCasingChanged(EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        ///   Gets and sets the starting point of the bytes selected in the hex box.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long SelectionStart
        {
            get { return _bytePos; }
            set
            {
                SetPosition(value, 0);
                ScrollByteIntoView();
                Invalidate();
            }
        }

        /// <summary>
        ///   Gets and sets the number of bytes selected in the hex box.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long SelectionLength
        {
            get { return _selectionLength; }
            set
            {
                SetSelectionLength(value);
                ScrollByteIntoView();
                Invalidate();
            }
        }

        private long _selectionLength;

        /// <summary>
        ///   Gets or sets the line info color. When this property is null, then ForeColor property is used.
        /// </summary>
        [DefaultValue(typeof(Color), "Empty"), Category("Hex"),
         Description("Gets or sets the line info color. When this property is null, then ForeColor property is used.")]
        public Color LineInfoForeColor
        {
            get { return _lineInfoForeColor; }
            set
            {
                _lineInfoForeColor = value;
                Invalidate();
            }
        }

        private Color _lineInfoForeColor = Color.Empty;

        /// <summary>
        ///   Gets or sets the background color for the selected bytes.
        /// </summary>
        [DefaultValue(typeof(Color), "Blue"), Category("Hex"),
         Description("Gets or sets the background color for the selected bytes.")]
        public Color SelectionBackColor
        {
            get { return _selectionBackColor; }
            set
            {
                _selectionBackColor = value;
                Invalidate();
            }
        }

        private Color _selectionBackColor = Color.Blue;

        /// <summary>
        ///   Gets or sets the foreground color for the selected bytes.
        /// </summary>
        [DefaultValue(typeof(Color), "White"), Category("Hex"),
         Description("Gets or sets the foreground color for the selected bytes.")]
        public Color SelectionForeColor
        {
            get { return _selectionForeColor; }
            set
            {
                _selectionForeColor = value;
                Invalidate();
            }
        }

        private Color _selectionForeColor = Color.White;

        /// <summary>
        ///   Gets or sets the visibility of a shadow selection.
        /// </summary>
        [DefaultValue(true), Category("Hex"), Description("Gets or sets the visibility of a shadow selection.")]
        public bool ShadowSelectionVisible
        {
            get { return _shadowSelectionVisible; }
            set
            {
                if (_shadowSelectionVisible == value)
                    return;
                _shadowSelectionVisible = value;
                Invalidate();
            }
        }

        private bool _shadowSelectionVisible = true;

        /// <summary>
        ///   Gets or sets the color of the shadow selection.
        /// </summary>
        /// <remarks>
        ///   A alpha component must be given! 
        ///   Default alpha = 100
        /// </remarks>
        [Category("Hex"), Description("Gets or sets the color of the shadow selection.")]
        public Color ShadowSelectionColor
        {
            get { return _shadowSelectionColor; }
            set
            {
                _shadowSelectionColor = value;
                Invalidate();
            }
        }

        private Color _shadowSelectionColor = Color.FromArgb(100, 60, 188, 255);

        /// <summary>
        ///   Gets the number bytes drawn horizontally.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int HorizontalByteCount { get; private set; }

        /// <summary>
        ///   Gets the number bytes drawn vertically.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int VerticalByteCount { get; private set; }

        /// <summary>
        ///   Gets the current line
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long CurrentLine { get; private set; }

        /// <summary>
        ///   Gets the current position in the current line
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long CurrentPositionInLine
        {
            get { return _currentPositionInLine; }
        }

        private int _currentPositionInLine;

        /// <summary>
        ///   Gets the a value if insertion mode is active or not.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool InsertActive
        {
            get { return _insertActive; }
            set
            {
                if (_insertActive == value)
                    return;
                _insertActive = value;
                // recreate caret
                DestroyCaret();
                CreateCaret();
                // raise change event
                OnInsertActiveChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        ///   Gets or sets the built-in context menu.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public BuiltInContextMenu BuiltInContextMenu
        {
            get { return _builtInContextMenu; }
        }

        private readonly BuiltInContextMenu _builtInContextMenu;
        #endregion

        #region Misc
        /// <summary>
        ///   Converts a byte array to a hex string. For example: {10,11} = "0A 0B"
        /// </summary>
        /// <param name = "data">the byte array</param>
        /// <returns>the hex string</returns>
        private string ConvertBytesToHex(IEnumerable<byte> data)
        {
            var sb = new StringBuilder();
            foreach (var b in data) sb.Append(ConvertByteToHex(b)).Append(" ");
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        /// <summary>
        ///   Converts the byte to a hex string. For example: "10" = "0A";
        /// </summary>
        /// <param name = "b">the byte to format</param>
        /// <returns>the hex string</returns>
        private string ConvertByteToHex(byte b)
        {
            var sB = b.ToString(_hexStringFormat, Thread.CurrentThread.CurrentCulture);
            if (sB.Length == 1)
                sB = "0" + sB;
            return sB;
        }

        /// <summary>
        ///   Converts the hex string to an byte array. The hex string must be separated by a space char ' '. If there is any invalid hex information in the string the result will be null.
        /// </summary>
        /// <param name = "hex">the hex string separated by ' '. For example: "0A 0B 0C"</param>
        /// <returns>the byte array. null if hex is invalid or empty</returns>
        private static byte[] ConvertHexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return null;
            hex = hex.Trim();
            var hexArray = hex.Split(' ');
            var byteArray = new byte[hexArray.Length];
            for (var i = 0; i < hexArray.Length; i++)
            {
                var hexValue = hexArray[i];
                byte b;
                var isByte = ConvertHexToByte(hexValue, out b);
                if (!isByte)
                    return null;
                byteArray[i] = b;
            }
            return byteArray;
        }

        private static bool ConvertHexToByte(string hex, out byte b)
        {
            return byte.TryParse(hex, NumberStyles.HexNumber,
                                 Thread.CurrentThread.CurrentCulture, out b);
        }

        private void SetPosition(long bytePos)
        {
            SetPosition(bytePos, _byteCharacterPos);
        }

        private void SetPosition(long bytePos, int byteCharacterPos)
        {
            _byteCharacterPos = byteCharacterPos;
            if (bytePos == _bytePos) return;
            _bytePos = bytePos;
            CheckCurrentLineChanged();
            CheckCurrentPositionInLineChanged();
            OnSelectionStartChanged(EventArgs.Empty);
        }

        private void SetSelectionLength(long selectionLength)
        {
            if (selectionLength == _selectionLength) return;
            _selectionLength = selectionLength;
            OnSelectionLengthChanged(EventArgs.Empty);
        }

        private void SetHorizontalByteCount(int value)
        {
            if (HorizontalByteCount == value)
                return;
            HorizontalByteCount = value;
            OnHorizontalByteCountChanged(EventArgs.Empty);
        }

        private void SetVerticalByteCount(int value)
        {
            if (VerticalByteCount == value)
                return;
            VerticalByteCount = value;
            OnVerticalByteCountChanged(EventArgs.Empty);
        }

        private void CheckCurrentLineChanged()
        {
            var currentLine = (long)Math.Floor((double)_bytePos / HorizontalByteCount) + 1; //TODO wtf
            if (_byteProvider == null && CurrentLine != 0)
            {
                CurrentLine = 0;
                OnCurrentLineChanged(EventArgs.Empty);
            }
            else if (currentLine != CurrentLine)
            {
                CurrentLine = currentLine;
                OnCurrentLineChanged(EventArgs.Empty);
            }
        }

        private void CheckCurrentPositionInLineChanged()
        {
            var gb = GetGridBytePoint(_bytePos, true);
            var currentPositionInLine = gb.X + 1;
            if (_byteProvider == null && _currentPositionInLine != 0)
            {
                _currentPositionInLine = 0;
                OnCurrentPositionInLineChanged(EventArgs.Empty);
            }
            else if (currentPositionInLine != _currentPositionInLine)
            {
                _currentPositionInLine = currentPositionInLine;
                OnCurrentPositionInLineChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        ///   Raises the InsertActiveChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnInsertActiveChanged(EventArgs e)
        {
            if (InsertActiveChanged != null)
                InsertActiveChanged(this, e);
        }

        /// <summary>
        ///   Raises the ReadOnlyChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnReadOnlyChanged(EventArgs e)
        {
            if (ReadOnlyChanged != null)
                ReadOnlyChanged(this, e);
        }

        /// <summary>
        ///   Raises the ByteProviderChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnByteProviderChanged(EventArgs e)
        {
            if (ByteProviderChanged != null)
                ByteProviderChanged(this, e);
        }

        /// <summary>
        ///   Raises the SelectionStartChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnSelectionStartChanged(EventArgs e)
        {
            if (SelectionStartChanged != null)
                SelectionStartChanged(this, e);
        }

        /// <summary>
        ///   Raises the SelectionLengthChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnSelectionLengthChanged(EventArgs e)
        {
            if (SelectionLengthChanged != null)
                SelectionLengthChanged(this, e);
        }

        /// <summary>
        ///   Raises the LineInfoVisibleChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnLineInfoVisibleChanged(EventArgs e)
        {
            if (LineInfoVisibleChanged != null)
                LineInfoVisibleChanged(this, e);
        }

        /// <summary>
        ///   Raises the StringViewVisibleChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnStringViewVisibleChanged(EventArgs e)
        {
            if (StringViewVisibleChanged != null)
                StringViewVisibleChanged(this, e);
        }

        /// <summary>
        ///   Raises the BorderStyleChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnBorderStyleChanged(EventArgs e)
        {
            if (BorderStyleChanged != null)
                BorderStyleChanged(this, e);
        }

        /// <summary>
        ///   Raises the UseFixedBytesPerLineChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnUseFixedBytesPerLineChanged(EventArgs e)
        {
            if (UseFixedBytesPerLineChanged != null)
                UseFixedBytesPerLineChanged(this, e);
        }

        /// <summary>
        ///   Raises the BytesPerLineChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnBytesPerLineChanged(EventArgs e)
        {
            if (BytesPerLineChanged != null)
                BytesPerLineChanged(this, e);
        }

        /// <summary>
        ///   Raises the VScrollBarVisibleChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnVScrollBarVisibleChanged(EventArgs e)
        {
            if (VScrollBarVisibleChanged != null)
                VScrollBarVisibleChanged(this, e);
        }

        /// <summary>
        ///   Raises the HexCasingChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnHexCasingChanged(EventArgs e)
        {
            if (HexCasingChanged != null)
                HexCasingChanged(this, e);
        }

        /// <summary>
        ///   Raises the HorizontalByteCountChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnHorizontalByteCountChanged(EventArgs e)
        {
            if (HorizontalByteCountChanged != null)
                HorizontalByteCountChanged(this, e);
        }

        /// <summary>
        ///   Raises the VerticalByteCountChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnVerticalByteCountChanged(EventArgs e)
        {
            if (VerticalByteCountChanged != null)
                VerticalByteCountChanged(this, e);
        }

        /// <summary>
        ///   Raises the CurrentLineChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnCurrentLineChanged(EventArgs e)
        {
            if (CurrentLineChanged != null)
                CurrentLineChanged(this, e);
        }

        /// <summary>
        ///   Raises the CurrentPositionInLineChanged event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnCurrentPositionInLineChanged(EventArgs e)
        {
            if (CurrentPositionInLineChanged != null)
                CurrentPositionInLineChanged(this, e);
        }

        /// <summary>
        ///   Raises the Copied event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnCopied(EventArgs e)
        {
            if (Copied != null)
                Copied(this, e);
        }

        /// <summary>
        ///   Raises the CopiedHex event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected virtual void OnCopiedHex(EventArgs e)
        {
            if (CopiedHex != null)
                CopiedHex(this, e);
        }

        /// <summary>
        ///   Raises the MouseDown event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            Debug.WriteLine("OnMouseDown()", "HexBox");
            if (!Focused)
                Focus();
            if (e.Button == MouseButtons.Left)
                SetCaretPosition(new Point(e.X, e.Y));
            base.OnMouseDown(e);
        }

        /// <summary>
        ///   Raises the MouseWhell event
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var linesToScroll = -(e.Delta * SystemInformation.MouseWheelScrollLines / 120);
            PerformScrollLines(linesToScroll);
            base.OnMouseWheel(e);
        }

        /// <summary>
        ///   Raises the Resize event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRectanglePositioning();
        }

        /// <summary>
        ///   Raises the GotFocus event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected override void OnGotFocus(EventArgs e)
        {
            Debug.WriteLine("OnGotFocus()", "HexBox");
            base.OnGotFocus(e);
            CreateCaret();
        }

        /// <summary>
        ///   Raises the LostFocus event.
        /// </summary>
        /// <param name = "e">An EventArgs that contains the event data.</param>
        protected override void OnLostFocus(EventArgs e)
        {
            Debug.WriteLine("OnLostFocus()", "HexBox");
            base.OnLostFocus(e);
            DestroyCaret();
        }

        private void ByteProviderLengthChanged(object sender, EventArgs e)
        {
            UpdateScrollSize();
        }
        #endregion
        public class HexboxHighlight
        {
            public HexboxHighlight(int start, int length, Color color)
            {
                _startIndex = start;
                _length = length;
                _color = color;
            }

            public int _startIndex;
            public int _length;
            public Color _color;

        }
    }
}