// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Pomelo.Storage.WebDAV.Lock
{
    public class SimpleWebDavLockManager : IWebDAVLockManager
    {
        private readonly Dictionary<Guid, Models.Lock> Locks = new Dictionary<Guid, Models.Lock>();
        private readonly AsyncSemaphore _lock = new AsyncSemaphore(1);
        private readonly long maxLockDurationSeconds;

        public SimpleWebDavLockManager(long maxLockDurationSeconds = 86400) 
        {
            this.maxLockDurationSeconds = maxLockDurationSeconds;
        }

        public string Schema => "urn:uuid";

        public async Task DeleteLockByUriAsync(
            string encodedUri,
            CancellationToken cancellationToken = default)
        {
            await ClearTimeoutLocksAsync();
            var tokens = new List<Guid>();
            foreach(var _lock in Locks.Values.Where(x => x.EncodedRelativeUri.StartsWith(encodedUri)))
            {
                tokens.Add(_lock.LockToken);
            }
            foreach(var token in tokens)
            {
                await UnlockAsync(token, cancellationToken);
            }
        }

        public async Task<IEnumerable<Models.Lock>> GetLocksAsync(
            string encodedUri,
            CancellationToken cancellationToken = default)
        {
            await ClearTimeoutLocksAsync();
            var ret = new List<Models.Lock>();
            foreach(var x in Locks.Values)
            {
                if (!encodedUri.StartsWith(x.EncodedRelativeUri))
                {
                    continue;
                }

                var postfix = encodedUri.Substring(x.EncodedRelativeUri.Length);
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
                        if (count == 0 && encodedUri == x.EncodedRelativeUri)
                        {
                            ret.Add(x);
                        }
                    }
                }
            }

            return ret;
        }

        public async Task<Models.Lock> LockAsync(
            string encodedUri, 
            int depth, 
            LockType type, 
            string owner = null,
            long timeoutSeconds = -1,
            CancellationToken cancellationToken = default)
        {
            await ClearTimeoutLocksAsync();

            if (timeoutSeconds > maxLockDurationSeconds || timeoutSeconds == -1)
            {
                timeoutSeconds = maxLockDurationSeconds;
            }
            List<Models.Lock> conflicted = null;
            if (depth == -1)
            {
                conflicted = Locks.Values.Where(x => x.EncodedRelativeUri.StartsWith(encodedUri)).ToList();
            }
            else
            {
                conflicted = new List<Models.Lock>();
                foreach (var x in Locks.Values.Where(x => x.EncodedRelativeUri.StartsWith(encodedUri)))
                {
                    var postfix = x.EncodedRelativeUri.Substring(encodedUri.Length);
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
                        conflicted.Add(x);
                    }
                }
            }

            if (type == LockType.Exclusive && conflicted.Count > 0 
                || type == LockType.Shared && conflicted.Any(x => x.Type == LockType.Exclusive))
            {
                throw new LockException("The encodedUri is already locked");
            }

            await _lock.WaitAsync();
            try
            {
                var locks = await GetLocksAsync(encodedUri, cancellationToken);
                if (locks.Any(x => x.Type == LockType.Exclusive))
                {
                    throw new LockException("The encodedUri is already exclusive locked");
                }

                if (type == LockType.Exclusive && locks.Any())
                {
                    throw new LockException("The encodedUri is already locked");
                }

                var webDavLock = new Models.Lock 
                {
                    Depth = depth,
                    Expire = DateTime.UtcNow.AddSeconds(timeoutSeconds),
                    Owner = owner,
                    Type = type,
                    EncodedRelativeUri = encodedUri,
                    RequestedTimeoutSeconds = timeoutSeconds
                };

                Locks[webDavLock.LockToken] = webDavLock;

                return webDavLock;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<Models.Lock> RefreshLock(
            Guid lockToken,
            long timeoutSeconds = -1,
            CancellationToken cancellationToken = default)
        {
            await ClearTimeoutLocksAsync();

            if (!Locks.ContainsKey(lockToken))
            {
                return null;
            }

            if (timeoutSeconds > maxLockDurationSeconds || timeoutSeconds == -1)
            {
                timeoutSeconds = maxLockDurationSeconds;
            }

            var _lock = Locks[lockToken];
            _lock.Expire = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            _lock.RequestedTimeoutSeconds = timeoutSeconds;
            return _lock;
        }

        public async Task UnlockAsync(Guid lockToken, CancellationToken cancellationToken = default)
        {
            await ClearTimeoutLocksAsync();

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

        private async Task ClearTimeoutLocksAsync()
        {
            try
            {
                var tokensToRemove = new List<Guid>();

                foreach (var x in Locks)
                {
                    if (x.Value.Expire.HasValue && x.Value.Expire.Value < DateTime.UtcNow)
                    {
                        tokensToRemove.Add(x.Key);
                    }
                }

                foreach (var token in tokensToRemove)
                {
                    Locks.Remove(token);
                }
            } 
            finally 
            {
            }
        }
    }

    public static class SimpleWebDavLockManagerExtensions
    {
        public static IServiceCollection AddSimpleWebDavLockManager(this IServiceCollection services, long maxLockDurationSeconds = 86400)
            => services.AddSingleton<IWebDAVLockManager, SimpleWebDavLockManager>(x => new SimpleWebDavLockManager(maxLockDurationSeconds));
    }
}
