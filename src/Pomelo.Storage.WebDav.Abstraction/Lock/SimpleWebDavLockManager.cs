namespace Pomelo.Storage.WebDav.Abstractions.Lock
{
    public class SimpleWebDavLockManager : IWebDAVLockManager
    {
        private readonly Dictionary<Guid, Models.Lock> Locks = new Dictionary<Guid, Models.Lock>();
        private readonly AsyncSemaphore _lock = new AsyncSemaphore(1);

        public async Task DeleteLockByUriAsync(
            string uri,
            CancellationToken cancellationToken = default)
        {
            uri = uri.Trim('/');

            var tokens = new List<Guid>();
            foreach(var _lock in Locks.Values.Where(x => x.Uri.StartsWith(uri)))
            {
                tokens.Add(_lock.LockToken);
            }

            foreach(var token in tokens)
            {
                await UnlockAsync(token, cancellationToken);
            }
        }

        public Task<IEnumerable<Models.Lock>> GetLocksAsync(
            string uri, 
            CancellationToken cancellationToken = default)
        {
            uri = uri.Trim('/');

            var ret = new List<Models.Lock>();

            foreach(var x in Locks.Values)
            {
                if (!uri.StartsWith(x.Uri))
                {
                    continue;
                }

                var postfix = uri.Substring(x.Uri.Length);
                if (x.Depth == -1)
                {
                    ret.Add(x);
                }
                else
                {
                    var count = 0;
                    for (var i = 0; i < postfix.Length; ++i)
                    {
                        if (postfix[i] == '/')
                        {
                            ++count;
                        }
                    }

                    if (count <= x.Depth)
                    {
                        ret.Add(x);
                    }
                }
            }

            return Task.FromResult(ret as IEnumerable<Models.Lock>);
        }

        public async Task<Models.Lock> LockAsync(
            string uri, 
            int depth, 
            LockType type, 
            string owner = null,
            int timeoutSeconds = 86400,
            CancellationToken cancellationToken = default)
        {
            uri = uri.Trim('/');
            IEnumerable<Models.Lock> conflicted = null;
            if (depth == -1)
            {
                conflicted = Locks.Values.Where(x => x.Uri.StartsWith(uri));
            }
            else
            {
                foreach(var x in Locks.Values.Where(x => x.Uri.StartsWith(uri)))
                {
                    var postfix = x.Uri.Substring(uri.Length);
                    var count = 0;
                    for (var i = 0; i < postfix.Length; ++i)
                    {
                        if (postfix[i] == '/')
                        {
                            ++count;
                        }
                    }

                    if (count >= depth)
                    {
                        throw new LockException("The uri is already locked");
                    }
                }

            }

            await _lock.WaitAsync();
            try
            {
                var locks = await GetLocksAsync(uri, cancellationToken);
                if (locks.Any(x => x.Type == LockType.Exclusive))
                {
                    throw new LockException("The uri is already exclusive locked");
                }

                if (type == LockType.Exclusive && locks.Any())
                {
                    throw new LockException("The uri is already locked");
                }

                var webDavLock = new Models.Lock 
                {
                    Depth = depth,
                    Expire = DateTime.UtcNow.AddSeconds(timeoutSeconds),
                    Owner = owner,
                    Type = type,
                    Uri = uri
                };

                Locks[webDavLock.LockToken] = webDavLock;

                return webDavLock;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task UnlockAsync(Guid lockToken, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync();
            try
            {
                if (Locks.ContainsKey(lockToken))
                {
                    Locks.Remove(lockToken);
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    public static class SimpleWebDavLockManagerExtensions
    {
        public static IServiceCollection AddSimpleWebDavLockManager(this IServiceCollection services)
            => services.AddSingleton<IWebDAVLockManager, SimpleWebDavLockManager>();
    }
}
