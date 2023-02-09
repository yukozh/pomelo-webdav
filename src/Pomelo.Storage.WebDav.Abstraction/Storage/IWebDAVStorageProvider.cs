// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Xml.Linq;
using Pomelo.Storage.WebDAV.Abstractions.Models;

namespace Pomelo.Storage.WebDAV.Abstractions.Storage
{
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

        Task MoveItemAsync(
            string fromDecodedRelativeUri,
            string destDecodedRelativeUri,
            bool overwrite,
            CancellationToken cancellationToken = default);

        Task CopyItemAsync(
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
