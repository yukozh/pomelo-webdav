namespace Pomelo.Storage.WebDav.Abstractions.Lock
{
    public enum LockType
    {
        Shared,
        Exclusive
    }

    public interface IWebDavLockManager
    {
        Task<IEnumerable<Models.Lock>> GetLocksAsync(string uri, CancellationToken cancellationToken = default);

        Task<Models.Lock> LockAsync(string uri, int depth, LockType type, string owner = null, int timeoutSeconds = 86400, CancellationToken cancellationToken = default);

        Task UnlockAsync(Guid lockToken, CancellationToken cancellationToken = default);
    }
}
