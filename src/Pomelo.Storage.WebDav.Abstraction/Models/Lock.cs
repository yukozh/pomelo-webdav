// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Pomelo.Storage.WebDAV.Abstractions.Lock;
using System.Diagnostics.CodeAnalysis;

namespace Pomelo.Storage.WebDAV.Abstractions.Models
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
