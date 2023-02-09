// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

namespace Pomelo.Storage.WebDAV.Abstractions.Models
{
    public class PatchPropertyResult
    {
        public List<string> PropertyNames { get; set; }

        public List<string> Namespaces { get; set; }

        public int StatusCode { get; set; }
    }
}
