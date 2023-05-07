// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Pomelo.Storage.WebDAV.Models;

namespace Pomelo.Storage.WebDAV.Storage
{
    public enum MoveOrCopyItemResult
    { 
        BadRequest,
        NotFound,
        Forbid,
        ResourceAlreadyExists,
        Locked,
        Conflict,
        Ok
    }

    public interface IWebDAVStorageProvider
    {
        Task<IEnumerable<Item>> GetItemsAsync(
            string decodedRelativeUri,
            CancellationToken cancellationToken = default);

        Task<Stream> GetFileReadStreamAsync(
            string decodedRelativeUri,
            CancellationToken cancellationToken = default);

        Task DeleteItemAsync(
            string decodedRelativeUri,
            CancellationToken cancellationToken = default);

        Task<Stream> GetFileWriteStreamAsync(
            string decodedRelativeUri,
            long? requestedBytes = null,
            CancellationToken cancellationToken = default);

        Task<bool> IsFileExistsAsync(
            string decodedRelativeUri,
            CancellationToken cancellationToken = default);

        Task<bool> IsDirectoryExistsAsync(
            string decodedRelativeUri,
            CancellationToken cancellationToken = default);

        Task CreateDirectoryAsync(
            string decodedRelativeUri,
            CancellationToken cancellationToken = default);

        Task<Item> GetItemAsync(
            string decodedRelativeUri,
            CancellationToken cancellationToken = default);

        Task<MoveOrCopyItemResult> MoveItemAsync(
            string fromDecodedRelativeUri,
            string destDecodedRelativeUri,
            bool overwrite,
            CancellationToken cancellationToken = default);

        Task<MoveOrCopyItemResult> CopyItemAsync(
            string fromDecodedRelativeUri,
            string destDecodedRelativeUri,
            bool overwrite,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<PatchPropertyResult>> PatchPropertyAsync(
            string decodedUri,
            IEnumerable<XElement> elementsToSet,
            IEnumerable<XElement> elementsToRemove,
            CancellationToken cancellationToken = default);
    }
}
