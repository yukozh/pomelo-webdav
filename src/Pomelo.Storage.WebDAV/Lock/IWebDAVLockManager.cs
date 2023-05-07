// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pomelo.Storage.WebDAV.Lock
{
    public enum LockType
    {
        Shared,
        Exclusive
    }

    public interface IWebDAVLockManager
    {
        string Schema { get; }

        Task<IEnumerable<Models.Lock>> GetLocksAsync(
            string encodedRelativeUri, 
            CancellationToken cancellationToken = default);

        Task<Models.Lock> LockAsync(
            string encodedUri,
            int depth,
            LockType type,
            string owner = null,
            long timeoutSeconds = -1,
            CancellationToken cancellationToken = default);

        Task<Models.Lock> RefreshLock(
            Guid lockToken, 
            long timeoutSeconds = -1, 
            CancellationToken cancellationToken = default);

        Task UnlockAsync(
            Guid lockToken, 
            CancellationToken cancellationToken = default);

        Task DeleteLockByUriAsync(
            string encodedRelativeUri,
            CancellationToken cancellationToken = default);
    }
}
