using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AnimCmd.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Hitbox
    {
        int _ID;
        int _Part;
        int _bone;
        float _damage;
        int _angle;
        int _knockbackGrowth;
        int _fixedKnockback;
        int _baseKnockback;
        float _radius;
        float _offsetX;
        float _offsetY;
        float _offsetZ;
        int _effect;
        float _tripChance;
        float _hitlag;
        float _sdiMultiplier;
        int _unk0;
        int _unk1;
        int _shieldDamage;
        int _sfxLevel;
        int _sfxType;
        int _groundAir;
        int _unk3;
        int _type;

        private VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SpecialHitbox
    {
        // Base Hitbox parameters
        int _ID;
        int _Part;
        int _bone;
        float _damage;
        int _angle;
        int _knockbackGrowth;
        int _fixedKnockback;
        int _baseKnockback;
        float _radius;
        float _offsetX;
        float _offsetY;
        float _offsetZ;
        int _effect;
        float _tripChance;
        float _hitlag;
        float _sdiMultiplier;
        int _unk0;
        int _unk1;
        int _shieldDamage;
        int _sfxLevel;
        int _sfxType;
        int _groundAir;
        int _unk3;
        int _type;

        // Special params
        int _unk4;
        int _unk6;
        int _unk7;
        int _unk8;
        int _unk9;
        int _unk10;
        int _unk11;
        int _unk12;
        int _unk13;
        int _unk14;
        int _unk15;
        int _unk16;
        int _unk17;
        int _unk18;
        int _unk19;
        int _unk20;
        int _unk21;

        private VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }
    }
}
