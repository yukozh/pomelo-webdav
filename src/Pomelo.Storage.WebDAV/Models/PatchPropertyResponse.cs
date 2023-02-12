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
