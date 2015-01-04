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
            return String.Format("0x{0:X8} @0x{1:X}", _identifier, _offset);
        }
    }
    
    public unsafe class SetBoneIntangability : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0xF13BFE8D;
        public const int ParamCount = 2;

        public int Bone { get { return _bone; } set { _bone = value; } }
        private int _bone;
        public int Setting { get { return _setting; } set { _setting = value; } }
        private int _setting;


        public override string GetFormated()
        {
            return String.Format("Set_Bone_Intangability(0x{0:X}, 0x{1:X})", Bone, Setting);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _bone = *(int*)(Header + 0x04);
            _setting = *(int*)(Header + 0x08);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new SetBoneIntangability() : null;
        }
        internal static string GetDictionaryName() { return "Set_Bone_Intangability()"; }
        
    }
    public unsafe class ResetBoneIntangability : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0xCEDC237E;
        public const int ParamCount = 1;

        public int Setting { get { return _setting; } set { _setting = value; } }
        private int _setting;


        public override string GetFormated()
        {
            return String.Format("Reset_Bone_Intangability(0x{0:X})", Setting);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _setting = *(int*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new ResetBoneIntangability() : null;
        }
        internal static string GetDictionaryName() { return "Reset_Bone_Intangability()"; }

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
            uint tag = *((uint*)addr);
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
    public unsafe class FrameSpeedMultiplier : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0x7172A764;
        public const int ParamCount = 1;

        public float Modifier { get { return _modifier; } set { _modifier = value; } }
        private float _modifier;


        public override string GetFormated()
        {
            return String.Format("Frame_Speed_Multiplier({0:0.0})", Modifier);
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
            return tag == Identifier ? new FrameSpeedMultiplier() : null;
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


        public override string GetFormated() { return "Script_End()"; }
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
            return tag == Identifier ? new ScriptEnd() : null;
        }
        internal static string GetDictionaryName() { return "Script_End()"; }

    }
    public unsafe class ExternalSubroutine : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0x9126EBA2;
        public const int ParamCount = 1;

        public uint Routine { get { return _routine; } }
        private uint _routine;

        public override string GetFormated() { return String.Format("External_Subroutine(0x{0:X8})", Routine); }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _routine = *(uint*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new ExternalSubroutine() : null;
        }
        internal static string GetDictionaryName() { return "External_Subroutine()"; }

    }
    public unsafe class Subroutine : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0xFA1BC28A;
        public const int ParamCount = 1;

        public uint Routine { get { return _routine; } }
        private uint _routine;

        public override string GetFormated() { return String.Format("Subroutine(0x{0:X8})", Routine); }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _routine = *(uint*)(Header + 0x04);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new Subroutine() : null;
        }
        internal static string GetDictionaryName() { return "Subroutine()"; }

    }
    #endregion

    #region Graphics
    public unsafe class ExternalGraphic : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0xFC061097;
        public const int ParamCount = 10;

        public int Graphic { get { return _graphic; } }
        private int _graphic;
        public int Bone { get { return _bone; } }
        private int _bone;

        public float TransZ { get { return _tz; } }
        private float _tz;
        public float TransY { get { return _ty; } }
        private float _ty;
        public float TransX { get { return _tx; } }
        private float _tx;

        public float RotZ { get { return _rz; } }
        private float _rz;
        public float RotY { get { return _ry; } }
        private float _ry;
        public float RotX { get { return _rx; } }
        private float _rx;
        public float Scale { get { return _scale; } }
        private float _scale;

        public int Anchored { get { return _anchor; } }
        private int _anchor;


        public override string GetFormated()
        {
            return String.Format("External_Graphic(0x{0:X8}, 0x{1:X}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, 0x{9:X})",
                                 Graphic, Bone, TransZ, TransY, TransX, RotZ, RotY, RotX, Scale, Anchored);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();
            _graphic = *(int*)(Header + 0x04);
            _bone = *(int*)(Header + 0x08);
            _tz = *(float*)(Header + 0x0c);
            _ty = *(float*)(Header + 0x10);
            _tx = *(float*)(Header + 0x14);
            _rz = *(float*)(Header + 0x18);
            _ry = *(float*)(Header + 0x1c);
            _rx = *(float*)(Header + 0x20);
            _scale = *(float*)(Header + 0x24);
            _anchor = *(int*)(Header + 0x28);
            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new ExternalGraphic() : null;
        }
        internal static string GetDictionaryName() { return "External_Graphic()"; }
    }

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
            uint tag = *((uint*)addr);
            return tag == Identifier ? new ColorOverlay() : null;
        }
        internal static string GetDictionaryName() { return "Color_Overlay()"; }
    }
    public unsafe class TerminateOverlays : Event
    {

        public VoidPtr Header { get { return WorkingSource.Address; } }

        public const uint Identifier = 0x5C3C583E;
        public const int ParamCount = 0;

        public override string GetFormated() { return "Terminate_Overlays()"; }
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
            return tag == Identifier ? new TerminateOverlays() : null;
        }
        internal static string GetDictionaryName() { return "Terminate_Overlays()"; }

    }
    #endregion

    #region Offensive Collisions
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

    public unsafe class GrabCollision : Event
    {
        public VoidPtr Header { get { return WorkingSource.Address; } }
        public const uint Identifier = 0x7B48FE1C;
        public const int ParamCount = 11;

        public int ID { get { return _id; } set { _id = value; } }
        private int _id;
        public int Bone { get { return _bone; } set { _bone = value; } }
        private int _bone;

        public float CollisionSize { get { return _collisionSize; } set { _collisionSize = value; } }
        private float _collisionSize;

        public float TranslationX { get { return _x; } set { _x = value; } }
        private float _x;
        public float TranslationY { get { return _y; } set { _y = value; } }
        private float _y;
        public float TranslationZ { get { return _z; } set { _z = value; } }
        private float _z;

        public int Unk0 { get { return _unk0; } set { _unk0 = value; } }
        private int _unk0;
        public int Unk1 { get { return _unk1; } set { _unk1 = value; } }
        private int _unk1;
        public int Unk2 { get { return _unk2; } set { _unk2 = value; } }
        private int _unk2;

        public float Unk3 { get { return _unk3; } set { _unk3 = value; } }
        private float _unk3;
        public float Unk4 { get { return _unk4; } set { _unk4 = value; } }
        private float _unk4;


        public override string GetFormated()
        {
            return String.Format("Grab_Collision(0x{0:X}, 0x{1:X}, {2}, {3}, {4}, {5}, 0x{6:X}, 0x{7:X}, 0x{8:X}, {9}, {10})",
                                    ID, Bone, CollisionSize, TranslationX, TranslationY, TranslationZ, Unk0, Unk1, Unk2, Unk3, Unk4);
        }
        public override int CalcSize() { return 0x04 + (ParamCount * 4); }

        public override void OnInit()
        {
            Size = CalcSize();

            _id = *(int*)(Header + 0x04);
            _bone = *(int*)(Header + 0x08);
            _collisionSize = *(float*)(Header + 0x0C);
            _x = *(float*)(Header + 0x10);
            _y = *(float*)(Header + 0x14);
            _z = *(float*)(Header + 0x18);
            _unk0 = *(int*)(Header + 0x1C);
            _unk1 = *(int*)(Header + 0x20);
            _unk2 = *(int*)(Header + 0x24);
            _unk3 = *(float*)(Header + 0x28);
            _unk4 = *(float*)(Header + 0x2C);

            if (_name == null)
                _name = GetFormated();
        }
        internal static Event TryParse(VoidPtr addr)
        {
            uint tag = *((uint*)addr);
            return tag == Identifier ? new GrabCollision() : null;
        }
        internal static string GetDictionaryName() { return "Grab_Collision()"; }


    }
    public unsafe class TerminateGrabs : Event
    {
        public const uint Identifier = 0xF3A464AC;
        public const int ParamCount = 0;


        public override string GetFormated() { return "Terminate_Grab_Collisions()"; }
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
            return tag == Identifier ? new TerminateGrabs() : null;
        }
        internal static string GetDictionaryName() { return "Terminate_Grab_Collisions()"; }

        
    }
    #endregion
}
