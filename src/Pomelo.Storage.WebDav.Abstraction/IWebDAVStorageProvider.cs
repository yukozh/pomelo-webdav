namespace Pomelo.Storage.WebDav.Abstractions
{
    public interface IWebDAVStorageProvider
    {
        Task<IEnumerable<Item>> GetItemsAsync(
            string path, 
            CancellationToken cancellationToken = default);

        Task<Stream> GetFileReadStreamAsync(
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
    }
}
