// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace SALT.Moveset
{
    public interface IScriptCollection
    {
        SortedList<uint, IScript> Scripts { get; set; }
        int Size { get; }

        byte[] GetBytes(System.IO.Endianness endian);
        void Export(string path);
        void Export(string path, System.IO.Endianness endian);
    }
}
