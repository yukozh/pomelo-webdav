// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Pomelo.Storage.WebDAV.Factory;
using Pomelo.Storage.WebDAV.Lock;
using Pomelo.Storage.WebDAV.Storage;

namespace Pomelo.Storage.WebDAV.E2ETests
{
    public class TestBase2 : IDisposable
    {
        protected string StoragePath { get; init; }

        protected string Drive => "Q:\\";

        protected WebApplication WebApplication { get; init; }

        public TestBase2() 
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel(x =>
            {
                x.Limits.MaxRequestBodySize = 1024 * 1024 * 1024 * 1024L;
            });
            builder.WebHost.UseUrls("http://localhost:8000");
            StoragePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "storage");
            if (Directory.Exists(StoragePath))
            {
                Directory.Delete(StoragePath, true);
            }
            Directory.CreateDirectory(StoragePath);
            builder.Services.AddDefaultWebDAVHttpHandlerFactory()
                .AddLocalDiskWebDAVStorageProvider(StoragePath)
                .AddSimpleWebDavLockManager();
            var app = builder.Build();

            app.MapGet("/", () => "Pomelo WebDAV server is running!");
            app.UseRouting();
            app.UseHttpOptionsMethodMiddleware();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPomeloWebDAV(x => x.Pattern = "/api/webdav/{*path}");
            });

            WebApplication = app;
            WebApplication.RunAsync();
            WaitForServerReadyAsync().GetAwaiter().GetResult();
            MountWebDAVDrive();
        }

        protected void MountWebDAVDrive()
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo 
                {
                    FileName = "net",
                    Arguments = "use Q: http://localhost:8000/api/webdav /user:admin 123456",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                process.WaitForExit();
            }
        }

        protected void UnmountWebDAVDrive()
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "use Q: /delete",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                process.WaitForExit();
            }
        }

        private async Task WaitForServerReadyAsync()
        {
            using var client = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
            while (true)
            {
                try {
                    using var response = await client.GetAsync("/");
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    throw new Exception("Server is not ready");
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
        }

        public virtual void Dispose()
        {
            UnmountWebDAVDrive();
            WebApplication?.DisposeAsync().GetAwaiter().GetResult();
            if (Directory.Exists(StoragePath))
            {
                Directory.Delete(StoragePath, true);
            }
        }
    }
}
