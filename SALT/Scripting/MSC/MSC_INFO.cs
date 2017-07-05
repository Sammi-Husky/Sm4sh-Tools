﻿// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace SALT.Scripting.MSC
{
    public static class MSC_INFO
    {
        #region Sizes
        public static Dictionary<uint, int> Sizes = new Dictionary<uint, int>()
        {
	    {0x00, 0},
            {0x02, 4},
            {0x03, 0},
            {0x04, 4},
            {0x05, 0},
            {0x06, 0},
            {0x07, 0},
            {0x08, 0},
            {0x09, 0},
            {0x0a, 4},
            {0x0b, 3},
	    {0x0c, 0},
            {0x0d, 2},
            {0x0e, 0},
            {0x0f, 0},
            {0x10, 0},
            {0x11, 0},
            {0x12, 0},
            {0x13, 0},
            {0x14, 3},
            {0x15, 3},
            {0x16, 0},
            {0x17, 0},
            {0x18, 0},
            {0x19, 0},
            {0x1a, 0},
	    {0x1b, 0},
            {0x1c, 3},
            {0x1d, 3},
            {0x1e, 3},
            {0x1f, 3},
	    {0x20, 3},
	    {0x21, 3},
	    {0x22, 3},
            {0x23, 3},
	    {0x24, 3},
            {0x25, 0},
            {0x26, 0},
            {0x27, 0},
            {0x28, 0},
            {0x29, 0},
            {0x2a, 0},
            {0x2b, 0},
            {0x2c, 1},
            {0x2d, 2},
            {0x2e, 4},
            {0x2f, 1},
            {0x30, 1},
            {0x31, 1},
	    {0x32, 0},
	    {0x33, 0},
            {0x34, 4},
            {0x35, 4},
            {0x36, 4},
            {0x37, 0},
            {0x38, 1},
            {0x39, 1},
            {0x3a, 0},
            {0x3b, 0},
            {0x3c, 0},
            {0x3d, 0},
            {0x3e, 0},
	    {0x3f, 3},
	    {0x40, 3},
            {0x41, 3},
            {0x42, 3},
            {0x43, 3},
            {0x44, 3},
            {0x45, 3},
            {0x46, 0},
            {0x47, 0},
            {0x48, 0},
            {0x49, 0},
            {0x4a, 0},
            {0x4b, 0},
            {0x4c, 0},
            {0x4d, 0}
        };
        #endregion
        #region Names
        public static Dictionary<uint, string> NAMES = new Dictionary<uint, string>()
        {
	    {0x00, "nop"},
            {0x02, "BeginSub"},
            {0x03, "End"},
            {0x04, "jump4"},
            {0x05, "jump5"},
            {0x06, "return6"},
            {0x07, "return7"},
            {0x08, "return8"},
            {0x09, "return9"},
            {0x0a, "pushInt"},
            {0x0b, "pushVar"},
	        {0x0c, "assert"},
            {0x0d, "pushShort"},
            {0x0e, "addi"},
            {0x0f, "subi"},
            {0x10, "mult"},
            {0x11, "divi"},
            {0x12, "modi"},
            {0x13, "negi"},
            {0x14, "inci"},
            {0x15, "deci"},
            {0x16, "and"},
            {0x17, "or"},
            {0x18, "not"},
            {0x19, "xor"},
            {0x1a, "shl"},
            {0x1b, "shr"},
            {0x1c, "set"},
            {0x1d, "addu"},
            {0x1e, "subu"},
            {0x1f, "mulu"},
            {0x20, "divu"},
            {0x21, "modu"},
            {0x22, "andu"},
            {0x23, "oru"},
            {0x24, "xoru"},
            {0x25, "equal"},
            {0x26, "notEqual"},
            {0x27, "lessThan"},
            {0x28, "lessOrEqual"},
            {0x29, "greater"},
            {0x2a, "greaterOrEqual"},
            {0x2b, "isZero"},
            {0x2c, "printf"},
            {0x2d, "sys"},
            {0x2e, "unk_2E"},
            {0x2f, "Call2"},
            {0x30, "call3"},
            {0x31, "Call4"},
	        {0x32, "push"},
	        {0x33, "pop"},
            {0x34, "if"},
            {0x35, "ifNot"},
            {0x36, "else"},
    	    {0x37, "assert_37"},
            {0x38, "intToFloat"},
            {0x39, "floatToInt"},
            {0x3a, "addf"},
            {0x3b, "subf"},
            {0x3c, "multf"},
            {0x3d, "divf"},
            {0x3e, "negf"},
            {0x3f, "finc"},
            {0x40, "fdec"},
            {0x41, "fset"},
            {0x42, "addfu"},
            {0x43, "subfu"},
            {0x44, "multfu"},
            {0x45, "divfu"},
            {0x46, "fGreaterThan"},
            {0x47, "fLessOrEqual"},
            {0x48, "fLessThan"},
            {0x49, "fNotEqual"},
            {0x4a, "fEquals"},
            {0x4b, "fGreaterOrEqual"},
            {0x4c, "assert_4C"},
	        {0x4d, "exit"}
        };
        #endregion
        #region Params

        #region Formats
        public static Dictionary<uint, string> FORMATS = new Dictionary<uint, string>()
        {
            {0x00, string.Empty},
            {0x02, "B,B,B,B"},
            {0x03, string.Empty},
            {0x04, "I"},
            {0x05, string.Empty},
            {0x06, string.Empty},
            {0x07, string.Empty},
            {0x08, string.Empty},
            {0x09, string.Empty},
            {0x0a, "I"},
            {0x0b, "B,B,B"},
            {0x0c, string.Empty},
            {0x0d, "H"},
            {0x0e, string.Empty},
            {0x0f, string.Empty},
            {0x10, string.Empty},
            {0x11, string.Empty},
            {0x12, string.Empty},
            {0x13, string.Empty},
            {0x14, "B,B,B"},
            {0x15, "B,B,B"},
            {0x16, string.Empty},
            {0x17, string.Empty},
            {0x18, string.Empty},
            {0x19, string.Empty},
            {0x1a, string.Empty},
            {0x1b, string.Empty},
            {0x1c, "B,B,B"},
            {0x1d, "B,B,B"},
            {0x1e, "B,B,B"},
            {0x1f, "B,B,B"},
            {0x20, "B,B,B"},
            {0x21, "B,B,B"},
            {0x22, "B,B,B"},
            {0x23, "B,B,B"},
            {0x24, "B,B,B"},
            {0x25, string.Empty},
            {0x26, string.Empty},
            {0x27, string.Empty},
            {0x28, string.Empty},
            {0x29, string.Empty},
            {0x2a, string.Empty},
            {0x2b, string.Empty},
            {0x2c, "B"},
            {0x2d, "B,B"},
            {0x2e, "I"},
            {0x2f, "B"},
            {0x30, "B"},
            {0x31, "B"},
            {0x32, string.Empty},
            {0x33, string.Empty},
            {0x34, "I"},
            {0x35, "I"},
            {0x36, "I"},
            {0x37, string.Empty},
            {0x38, "B"},
            {0x39, "B"},
            {0x3a, string.Empty},
            {0x3b, string.Empty},
            {0x3c, string.Empty},
            {0x3d, string.Empty},
            {0x3e, string.Empty},
            {0x3f, "B,B,B"},
            {0x40, "B,B,B"},
            {0x41, "B,B,B"},
            {0x42, "B,B,B"},
            {0x43, "B,B,B"},
            {0x44, "B,B,B"},
            {0x45, "B,B,B"},
            {0x46, string.Empty},
            {0x47, string.Empty},
            {0x48, string.Empty},
            {0x49, string.Empty},
            {0x4a, string.Empty},
            {0x4b, string.Empty},
            {0x4c, string.Empty},
            {0x4d, string.Empty}
        };
        #endregion
        #region SYNTAX
        public static Dictionary<uint, string> SYNTAX = new Dictionary<uint, string>()
        {
            {0x00, string.Empty},
            {0x02, string.Empty},
            {0x03, string.Empty},
            {0x04, string.Empty},
            {0x05, string.Empty},
            {0x06, string.Empty},
            {0x07, string.Empty},
            {0x08, string.Empty},
            {0x09, string.Empty},
            {0x0a, string.Empty},
            {0x0b, string.Empty},
            {0x0c, string.Empty},
            {0x0d, string.Empty},
            {0x0e, string.Empty},
            {0x0f, string.Empty},
            {0x10, string.Empty},
            {0x11, string.Empty},
            {0x12, string.Empty},
            {0x13, string.Empty},
            {0x14, string.Empty},
            {0x15, string.Empty},
            {0x16, string.Empty},
            {0x17, string.Empty},
            {0x18, string.Empty},
            {0x19, string.Empty},
            {0x1a, string.Empty},
            {0x1b, string.Empty},
            {0x1c, "Global,unk,ID"},
            {0x1d, string.Empty},
            {0x1e, string.Empty},
            {0x1f, string.Empty},
            {0x20, string.Empty},
            {0x21, string.Empty},
            {0x22, string.Empty},
            {0x23, string.Empty},
            {0x24, string.Empty},
            {0x25, string.Empty},
            {0x26, string.Empty},
            {0x27, string.Empty},
            {0x28, string.Empty},
            {0x29, string.Empty},
            {0x2a, string.Empty},
            {0x2b, string.Empty},
            {0x2c, string.Empty},
            {0x2d, "ParamCount,ID"},
            {0x2e, string.Empty},
            {0x2f, "Function"},
            {0x30, string.Empty},
            {0x31, string.Empty},
            {0x32, string.Empty},
            {0x33, string.Empty},
            {0x34, string.Empty},
            {0x35, string.Empty},
            {0x36, string.Empty},
            {0x37, string.Empty},
            {0x38, string.Empty},
            {0x39, string.Empty},
            {0x3a, string.Empty},
            {0x3b, string.Empty},
            {0x3c, string.Empty},
            {0x3d, string.Empty},
            {0x3e, string.Empty},
            {0x3f, string.Empty},
            {0x40, string.Empty},
            {0x41, string.Empty},
            {0x42, string.Empty},
            {0x43, string.Empty},
            {0x44, string.Empty},
            {0x45, string.Empty},
            {0x46, string.Empty},
            {0x47, string.Empty},
            {0x48, string.Empty},
            {0x49, string.Empty},
            {0x4a, string.Empty},
            {0x4b, string.Empty},
            {0x4c, string.Empty},
            {0x4d, string.Empty},
        };
        #endregion
        #endregion
    }
}
