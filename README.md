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

[https://github.com/yukozh/pomelo-webdav/tree/main/sample/Pomelo.Storage.WebDAV.Sample](https://github.com/yukozh/pomelo-webdav/tree/main/sample/Pomelo.Storage.WebDAV.Sample)

## Contact Me

- Email: hi@yuko.me
- QQ: 911574351

## License

[MIT](/LICENSE)
