using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using AnimCmd.Structs;
using System.Windows.Forms;

namespace AnimCmd.Classes
{
    public unsafe class Event
    {
        public int Size;

        public DataSource WorkingSource { get { return _workingSource; } }
        private DataSource _workingSource;


        public string CommandName { get { return _name; } set { _name = value; } }
        public string _name; 

        public virtual int CalcSize() { return 0; }
        public virtual string GetFormated() { return String.Empty; }

        public virtual void Init(DataSource source)
        {
            _workingSource = source;
            OnInit();
        }
        public virtual void OnInit() { }
    }
    public unsafe class UnknownEvent : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public uint _identifier;
        public int _offset;

        public override void OnInit()
        {
            _identifier = (*(uint*)Header);

            if (_name == null)
                _name = GetFormated();
        }
        public override string GetFormated()
        {
            return String.Format("0x{0:X8} 0x{1:X}", _identifier, _offset);
        }
    }

    #region Movement
    public unsafe class AddSetMomentum : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0xA4DE708D; 
        public const int ParamCount = 3;

        public float Horizontal { get { return _horizontal; } }
        private float _horizontal;
        public float Vertical { get { return _vertical; } }
        private float _vertical;
        public int Mode { get { return _mode; } }
        private int _mode;

        public override string GetFormated() { return String.Format("Add/Set_Momentum(Horizontal: {0}, Vertical: {1}, Add/Set: {2})", _horizontal, _vertical, _mode); }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _horizontal = *(float*)(Header + 0x04);
            _vertical = *(float*)(Header + 0x08);
            _mode = *(int*)(Header + 0x0c);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr) 
        {
            uint tag =*((uint*)addr);
            return tag == Identifier ? new AddSetMomentum() : null; 
        }
        internal static string GetDictionaryName() { return "Add/Set_Momentum()"; }
    }
    #endregion

    #region Frame Manipulation
    public unsafe class AsynchronousTimer : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0x42ACFE7D;
        public const int ParamCount = 1;

        public float Frames { get { return _frames; } set { _frames = value; } }
        private float _frames;


        public override string GetFormated()
        {
            return String.Format("Asynchronous_Timer({0:0.0})", Frames);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _frames = *(float*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new AsynchronousTimer() : null;
        }
        internal static string GetDictionaryName() { return "Asynchronous_Timer()"; }
    }
    public unsafe class SynchronousTimer : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0x4B7B6E51;
        public const int ParamCount = 1;

        public float Frames { get { return _frames; } set { _frames = value; } }
        private float _frames;


        public override string GetFormated()
        {
            return String.Format("Synchronous_Timer({0:0.0})", Frames);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _frames = *(float*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new SynchronousTimer() : null;
        }
        internal static string GetDictionaryName() { return "Synchronous_Timer()"; }
    }
    public unsafe class FrameSpeedModifier : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0x7172A764;
        public const int ParamCount = 1;

        public float Modifier { get { return _modifier; } set { _modifier = value; } }
        private float _modifier;


        public override string GetFormated()
        {
            return String.Format("Frame_speed_modifier({0:0.0})", Modifier);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _modifier = *(float*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new FrameSpeedModifier() : null;
        }
        internal static string GetDictionaryName() { return "Frame_Speed_Modifier()"; }
    }
    #endregion

    #region Flow of Exectution

    public unsafe class SetLoop : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0x0EB375E3;
        public const int ParamCount = 1;

        public int Iterations { get { return _iterations; } }
        private int _iterations;


        public override string GetFormated()
        {
            return String.Format("Set_Loop({0})", _iterations);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _iterations = *(int*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new SetLoop() : null;
        }
        internal static string GetDictionaryName() { return "Set_Loop()"; }
    }
    public unsafe class ScriptEnd : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0x5766F889; 
        public const int ParamCount = 0;
        

        public override string GetFormated() {return "Script_End()";}
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr) 
        {
            uint tag =*((uint*)addr);
            return tag == Identifier ? new ScriptEnd() : null; 
        }
        internal static string GetDictionaryName() { return "Script_End()"; }

    }
    public unsafe class ChangeAction : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0x9126EBA2; 
        public const int ParamCount = 1;

        public uint Action { get { return _action; } }
        private uint _action;

        public override string GetFormated() {return String.Format("Change_Action(0x{0:X8})",Action);}
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _action = *(uint*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr) 
        {
            uint tag =*((uint*)addr);
            return tag == Identifier ? new ChangeAction() : null; 
        }
        internal static string GetDictionaryName() {return "Change_Action()";}
        
    }

    #endregion

    #region Graphics
    public unsafe class ColorOverlay : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0x589A9DB3; 
        public const int ParamCount = 4;

        public float Red { get { return _r; } }
        private float _r;
        public float Green { get { return _g; } }
        private float _g;
        public float Blue { get { return _b; } }
        private float _b;
        public float Alpha { get { return _a; } }
        private float _a;

        public override string GetFormated() { return String.Format("Color_Overlay(Red: {0}, Green: {1}, Blue: {2}, Alpha: {3})", _r, _g, _b, _a); }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _r = *(float*)(Header + 0x04);
            _g = *(float*)(Header + 0x08);
            _b = *(float*)(Header + 0x0c);
            _a = *(float*)(Header + 0x10);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr) 
        {
            uint tag =*((uint*)addr);
            return tag == Identifier ? new ColorOverlay() : null; 
        }
        internal static string GetDictionaryName() { return "Color_Overlay()"; }
    }
    public unsafe class TerminateOverlays : Event
    {

        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0x5C3C583E; 
        public const int ParamCount = 0;

        public override string GetFormated() { return "Terminate_Overlays()";}
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr) 
        {
            uint tag =*((uint*)addr);
            return tag == Identifier ? new TerminateOverlays() : null; 
        }
        internal static string GetDictionaryName() { return "Terminate_Overlays()"; }
        
    }
    #endregion

    #region Hitbox related
    public unsafe class HitboxCmd : Event
    {
        public Hitbox Header { get { return *(Hitbox*)(WorkingSource.Address + 0x04); } }

        public const uint Identifier = 0xB738EABD;
        public const int ParamCount = 0x18;

        public override string GetFormated()
        {
            string Formated = "Hitbox(";
            foreach (var field in typeof(Hitbox).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                Formated += field.GetValue(Header) + ", ";

            return Formated + ")";
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new HitboxCmd() : null;
        }
        internal static string GetDictionaryName() { return "Hitbox()"; }

    }
    public unsafe class SpecialHitboxCmd : Event
    {
        public SpecialHitbox Header { get { return *(SpecialHitbox*)(WorkingSource.Address + 0x04); } }

        public const uint Identifier = 0x14FCC7E4;
        public const int ParamCount = 0x29;

        public override string GetFormated()
        {
            string Formated = "Special_Hitbox(";
            foreach (var field in typeof(SpecialHitbox).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                Formated += field.GetValue(Header) + ", ";

            return Formated + ")";
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            if (_name == null)
                _name = GetFormated();
        }

        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new SpecialHitboxCmd() : null;
        }
        internal static string GetDictionaryName() { return "Special_Hitbox()"; }
    }
    public unsafe class RemoveAllHitboxes : Event
    {
        public const uint Identifier = 0x9245E1A8;
        public const int ParamCount = 0;


        public override string GetFormated() { return "Terminate_Hitboxes()"; }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new RemoveAllHitboxes() : null;
        }
        internal static string GetDictionaryName() { return "Terminate_Hitboxes()"; }
    }
#endregion
}
