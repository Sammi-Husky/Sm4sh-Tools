// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace SALT.Scripting
{
    public interface IScript : IEnumerable<ICommand>
    {
        byte[] GetBytes(System.IO.Endianness endian);
        int Size { get; }
        List<ICommand> Commands { get; set; }
        string Deserialize();
        void Serialize(string text);
        void Clear();
    }
}
