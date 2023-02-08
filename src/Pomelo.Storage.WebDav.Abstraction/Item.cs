namespace Pomelo.Storage.WebDav.Abstractions
{
    public class Item
    {
        public string Href { get; set; }

        public ItemProperties Properties { get; set; }
    }

    public enum ItemType
    {
        RootDirectory,
        Directory,
        File
    }

    public class ItemProperties
    { 
        public ItemLock SupportedLock { get; set; }

        public ItemType ResourceType { get; set; }

        public long ContentLength { get; set; } = 0;

        public string Etag { get; set; } = "";

        public DateTime? CreationTime { get; set; }

        public DateTime? LastModified { get; set; }
    }

    public class ItemLock
    { 
        public IEnumerable<string> LockScopes { get; set; }

        public IEnumerable<string> LockTypes { get; set; }
    }
}
