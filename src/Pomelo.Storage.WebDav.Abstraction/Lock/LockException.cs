namespace Pomelo.Storage.WebDav.Abstractions.Lock
{
    public class LockException : Exception
    {
        public LockException(string message) : base(message) { }
    }
}
