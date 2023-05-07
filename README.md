# Pomelo.Storage.WebDAV

## Package

[https://www.nuget.org/packages/Pomelo.Storage.WebDAV](https://www.nuget.org/packages/Pomelo.Storage.WebDAV)

## Getting Started

### 1. Project Configuration

``` xml
<PackageReference Include="Pomelo.Storage.WebDAV" Version="1.0.0-*" />
```

### 2. Services Configuration

```c#
// Add Services
builder.Services.AddDefaultWebDAVHttpHandlerFactory()
    .AddLocalDiskWebDAVStorageProvider(storagePath)
    .AddSimpleWebDavLockManager();

// Using Middlewares
app.UseRouting();
app.UseEndpoints(endpoints => 
{
    endpoints.MapPomeloWebDAV("/{*path}");
});
```

### 3. Sample Application

[Pomelo.Storage.WebDAV.Sample](https://github.com/yukozh/pomelo-webdav/tree/main/sample/Pomelo.Storage.WebDAV.Sample)

## Customization

### 1. Storage

To implement `IWebDAVStorageProvider` to hook the physical storage to anywhere like distributed file system. The interface contains some file operations as bellow. 

```c#
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
```

There is a sample provider which stores file in local disk, [LocalDiskWebDAVStorageProvider.cs](https://github.com/yukozh/pomelo-webdav/blob/main/src/Pomelo.Storage.WebDAV/Storage/LocalDiskWebDAVStorageProvider.cs).

### 2. Lock

WebDAV supports `Shared` locks and `Exclusive` locks in the protocol. By implementing `IWebDAVLockManager` to define your own locking process. 

```c#
public interface IWebDAVLockManager
{
    string Schema { get; }

    Task<IEnumerable<Models.Lock>> GetLocksAsync(string encodedRelativeUri, CancellationToken cancellationToken = default);

    Task<Models.Lock> LockAsync(
        string encodedUri,
        int depth,
        LockType type,
        string owner = null,
        long timeoutSeconds = -1,
        CancellationToken cancellationToken = default);

    Task UnlockAsync(Guid lockToken, CancellationToken cancellationToken = default);

    Task DeleteLockByUriAsync(string encodedRelativeUri, CancellationToken cancellationToken = default);
}
```

You can also refer to an In-memory locks implementation, [SimpleWebDavLockManager.cs](https://github.com/yukozh/pomelo-webdav/blob/main/src/Pomelo.Storage.WebDAV/Lock/SimpleWebDavLockManager.cs)

### 3. WebDAV HTTP Methods

There is a `WebDAVHttpHandlerFactory` in this middleware. The factory will generate `WebDAVContext` to handle client requests. If you want to override the default behaviors, implement yoru own `IWebDAVHttpHandler`. There are many methods defined as bellow.

```c#
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
```

You can also define a class inherit from `DefaultWebDAVHttpHandler` to override existed methods. [DefaultWebDAVHttpHandler.cs](https://github.com/yukozh/pomelo-webdav/blob/main/src/Pomelo.Storage.WebDAV/Http/DefaultWebDAVHttpHandler.cs)

## Authentication & Authorization

You can use ASP.NET Core Authentication & Authorization middlewares with Pomelo.Storage.WebDAV. Developers are able to access `ClaimsPrincipal` from `WebDAVContext.User`.

## Contact Me

- Email: hi@yuko.me
- QQ: 911574351

## License

[MIT](/LICENSE)
