// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Threading.Tasks;

namespace Pomelo.Storage.WebDAV.Http
{
    public interface IWebDAVHttpHandler
    {
        Task GetAsync();

        Task PutAsync();

        Task DeleteAsync();

        Task PropFindAsync();

        Task PropPatchAsync();

        Task HeadAsync();

        Task OptionsAsync();

        Task LockAsync();

        Task UnlockAsync();

        Task MkcolAsync();

        Task MoveAsync();

        Task CopyAsync();

        Task<bool> IsAbleToMoveOrCopyAsync();

        Task<bool> IsAbleToWriteAsync(string uri = null);

        Task<bool> IsAbleToReadAsync();
    }
}
