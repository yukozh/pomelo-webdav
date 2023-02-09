// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pomelo.Storage.WebDAV.Factory;
using Pomelo.Storage.WebDAV.Lock;
using Pomelo.Storage.WebDAV.Storage;

namespace Pomelo.Storage.WebDAV.Sample
{
    [ExcludeFromCodeCoverage]
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
            builder.Services.AddDefaultWebDAVHttpHandlerFactory()
                .AddLocalDiskWebDAVStorageProvider(storagePath)
                .AddSimpleWebDavLockManager();
            var app = builder.Build();
            app.MapGet("/", () => "Pomelo WebDAV server is running!");
            app.UseRouting();
            app.UseEndpoints(endpoints => 
            {
                endpoints.MapPomeloWebDAV("/{*path}");
            });
            app.Run();
        }
    }
}