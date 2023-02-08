using Pomelo.Storage.WebDav.Abstractions.Lock;

namespace Pomelo.Storage.WebDav.Abstractions.Models
{
    public class Lock
    {
        public string Uri { get; set; }

        public Guid LockToken { get; set; } = Guid.NewGuid();

        public DateTime? Expire { get; set; }

        public int Depth { get; set; } = -1;

        public LockType Type { get; set; }

        public string Owner { get; set; }
    }
}
