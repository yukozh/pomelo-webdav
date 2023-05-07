// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Xml.Linq;

namespace Pomelo.Storage.WebDAV.Models
{
    public class PatchPropertyResponse
    {
        public ResponseStatus Status { get; set; }

        public IEnumerable<XElement> Properties { get; set; }
    }
}
