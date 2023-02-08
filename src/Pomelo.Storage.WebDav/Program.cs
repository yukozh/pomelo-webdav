using Pomelo.Storage.WebDav.Abstractions;
using System.Reflection;

namespace Pomelo.Storage.WebDav
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var storagePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "storage");
            builder.Services.AddLocalDiskWebDAVStorageProvider(storagePath);
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