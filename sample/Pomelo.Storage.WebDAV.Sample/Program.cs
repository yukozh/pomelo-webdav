// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Pomelo.Storage.WebDAV.Factory;
using Pomelo.Storage.WebDAV.Lock;
using Pomelo.Storage.WebDAV.Sample.Authentication;
using Pomelo.Storage.WebDAV.Sample.WebDAVMiddlewares;
using Pomelo.Storage.WebDAV.Storage;

namespace Pomelo.Storage.WebDAV.Sample
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        internal static IConfiguration Configuration;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseKestrel(x => 
            {
                x.Limits.MaxRequestBodySize = 1024 * 1024 * 1024 * 1024L;
            });
            Configuration = builder.Configuration;
            var storagePath = builder.Configuration["StoragePath"];
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }
            builder.Services.AddDefaultWebDAVHttpHandlerFactory()
                .AddLocalDiskWebDAVStorageProvider(storagePath)
                .AddSimpleWebDavLockManager()
                .AddAuthorization()
                .AddBasicAuthenticationHandler()
                .AddSampleMiddleware()
                .AddBasicAuthMiddleware();

            var app = builder.Build();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/", () => "Pomelo WebDAV server is running!");
            app.UseHttpOptionsMethodMiddleware();
            app.UseEndpoints(endpoints => 
            {
                endpoints.MapPomeloWebDAV(x => x.Pattern = "/api/webdav/{*path}");
            });
            app.Run();
        }
    }
}