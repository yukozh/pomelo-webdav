// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Pomelo.Storage.WebDAV.Lock;

namespace Pomelo.Storage.WebDAV.Models
{
    [ExcludeFromCodeCoverage]
    public class Lock
    {
        public string EncodedRelativeUri { get; set; }

        public Guid LockToken { get; set; } = Guid.NewGuid();

        public long RequestedTimeoutSeconds { get; set; }

        public DateTime? Expire { get; set; }

        public int Depth { get; set; } = -1;

        public LockType Type { get; set; }

        public string Owner { get; set; }
    }
}
