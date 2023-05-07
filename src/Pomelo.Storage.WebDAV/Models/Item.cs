// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;

namespace Pomelo.Storage.WebDAV.Models
{
    public class Item
    {
        public string Href { get; set; }

        public string DisplayName { get; set; }

        public ItemProperties Properties { get; set; }

        public int Depth { get; set; }
    }

    public enum ItemType
    {
        Directory,
        File
    }

    public class ItemProperties
    {
        public ItemType ResourceType { get; set; }

        public long ContentLength { get; set; } = 0;

        public string Etag { get; set; } = "";

        public DateTime? CreationTime { get; set; }

        public DateTime? LastModified { get; set; }
    }
}
