using System.Xml.Linq;
using Pomelo.Storage.WebDav.Abstractions.Models;

namespace Pomelo.Storage.WebDav.Abstractions.Storage
{
    public interface IWebDAVStorageProvider
    {
        Task<IEnumerable<Item>> GetItemsAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task<Stream> GetFileReadStreamAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task DeleteItemAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task<Stream> GetFileWriteStreamAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task<bool> IsFileExistsAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task<bool> IsDirectoryExistsAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task CreateDirectoryAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task<Item> GetItemAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task MoveItemAsync(
            string fromPath,
            string destPath,
            bool overwrite,
            CancellationToken cancellationToken = default);

        Task CopyItemAsync(
            string fromPath,
            string destPath,
            bool overwrite,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<PatchPropertyResult>> PatchPropertyAsync(
            string path,
            IEnumerable<XElement> elementsToSet,
            IEnumerable<XElement> elementsToRemove,
            CancellationToken cancellationToken = default);
    }
}
