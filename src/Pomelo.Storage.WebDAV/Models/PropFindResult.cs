// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Pomelo.Storage.WebDAV.Models
{
    public class PropFindResult
    {
        public string Href { get; set; }

        public string Name => Path.GetFileName(Href);

        public string Extension => Path.GetExtension(Href);

        public ItemType Type => PropStat
            .Properties
            .DescendantsAndSelf("{DAV:}resourcetype")
            .First()
            .Descendants("{DAV:}collection")
            .Any() 
            ? ItemType.Directory 
            : ItemType.File;

        public PropFindResultPropStat PropStat { get; set; }
    }

    public class PropFindResultPropStat
    {
        public ResponseStatus Status { get; set; }

        public IEnumerable<XElement> Properties { get; set; }
    }
}
