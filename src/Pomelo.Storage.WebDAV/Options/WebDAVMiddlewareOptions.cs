// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

namespace Pomelo.Storage.WebDAV.Options
{
    public class WebDAVMiddlewareOptions
    {
        public string Pattern { get; set; } = "/{*path}";

        public bool AllowReadExclusiveLockedResources { get; set; }
    }
}
