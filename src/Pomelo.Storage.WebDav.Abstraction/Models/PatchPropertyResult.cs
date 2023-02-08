namespace Pomelo.Storage.WebDav.Abstractions.Models
{
    public class PatchPropertyResult
    {
        public List<string> PropertyNames { get; set; }

        public List<string> Namespaces { get; set; }

        public int StatusCode { get; set; }
    }
}
