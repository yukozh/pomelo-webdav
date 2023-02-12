// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

namespace Pomelo.Storage.WebDAV.Models
{
    public class LockResult
    {
        public string Href { get; set; }

        public long TimeoutSeconds { get; set; }

        public string LockRoot { get; set; }

        public int Depth { get; set; }
        
        public string LockToken { get; set; }
    }
}
