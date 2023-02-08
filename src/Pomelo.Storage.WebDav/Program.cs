using Pomelo.Storage.WebDav.Abstractions;
using Pomelo.Storage.WebDav.Abstractions.Lock;
using Pomelo.Storage.WebDav.Abstractions.Storage;
using System.Reflection;

namespace Pomelo.Storage.WebDav
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseKestrel(x => 
            {
                x.Limits.MaxRequestBodySize = 1024 * 1024 * 1024 * 1024L;
            });
            var storagePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "storage");
            builder.Services.AddLocalDiskWebDAVStorageProvider(storagePath)
                .AddSimpleWebDavLockManager();
            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");
            app.UseRouting();
            app.UseEndpoints(endpoints => 
            {
                endpoints.MapWebDAV("/{*path}");
            });
            app.Run();
        }
    }
}