// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace SALT.Moveset
{
    public interface ICommand
    {
        List<object> Parameters { get; set; }
        uint Ident { get; set; }
        int Size { get; }
        string Name { get; }

        string ToString();
        byte[] GetBytes(System.IO.Endianness endian);
    }
}
