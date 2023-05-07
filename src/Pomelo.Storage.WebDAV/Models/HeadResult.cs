// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;

namespace Pomelo.Storage.WebDAV.Models
{
    public class HeadResult
    {
        public string Etag { get; set; }

        public DateTime? LastModified { get; set; }

        public string ContentType { get; set; }

        public long ContentLength { get; set; }

        public string AcceptRanges { get; set; }
    }
}
