namespace Pomelo.Storage.WebDAV.Models
{
    public class Property
    {
        public string Name { get; set; }

        public string Namespace { get; set; }
    }

    public class PropertyValue : Property
    { 
        public string Value { get; set; }
    }
}
