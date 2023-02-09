// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Pomelo.Storage.WebDAV.Lock
{
    [ExcludeFromCodeCoverage]
    public class LockException : Exception
    {
        public LockException(string message) : base(message) { }
    }
}
