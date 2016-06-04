// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace SALT.PARAMS
{
    public interface IParamCollection
    {
        List<ParamEntry> Values { get; set; }

        byte[] GetBytes();

        void Add(ParamEntry value);

        void Clear();
    }
}
