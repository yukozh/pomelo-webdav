// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Pomelo.Storage.WebDAV.Abstractions.Factory;
using Pomelo.Storage.WebDAV.Abstractions.Lock;

namespace Pomelo.Storage.WebDAV.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private readonly RequestDelegate _next;

        public WebDAVMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var method = httpContext.Request.Method.ToUpper();

            httpContext.Response.Headers.Server = "Pomelo WebDAV Server";

            var factory = httpContext.RequestServices.GetRequiredService<IWebDAVHttpHandlerFactory>();
            var handler = factory.CreateHandler(httpContext);
            var lockManager = httpContext.RequestServices.GetRequiredService<IWebDAVLockManager>();

            switch (method)
            {
                case "OPTIONS":
                    await handler.OptionsAsync();
                    return;
                case "HEAD":
                    await handler.HeadAsync();
                    break;
                case "GET":
                    if (await handler.IsAbleToReadAsync())
                    {
                        await handler.GetAsync();
                    }
                    return;
                case "PUT":
                case "POST":
                    if (await handler.IsAbleToWriteAsync())
                    {
                        await handler.PutAsync();
                    }
                    return;
                case "DELETE":
                    if (await handler.IsAbleToWriteAsync())
                    {
                        await handler.DeleteAsync();
                    }
                    break;
                case "MKCOL":
                    if (await handler.IsAbleToWriteAsync())
                    {
                        await handler.MkcolAsync();
                    }
                    break;
                case "PROPFIND":
                    await handler.PropFindAsync();
                    return;
                case "MOVE":
                    if (await handler.IsAbleToMoveOrCopyAsync())
                    {
                        await handler.MoveAsync();
                    }
                    return;
                case "COPY":
                    if (await handler.IsAbleToMoveOrCopyAsync())
                    {
                        await handler.CopyAsync();
                    }
                    return;
                case "PROPPATCH":
                    if (await handler.IsAbleToWriteAsync())
                    {
                        await handler.PropPatchAsync();
                    }
                    return;
                case "LOCK":
                    await handler.LockAsync();
                    return;
                case "UNLOCK":
                    await handler.UnlockAsync();
                    return;
                default:
                    await _next(httpContext);
                    return;
            }
        }
    }

    public static class WebDAVMiddlewareExtensions
    {
        public static IEndpointConventionBuilder MapPomeloWebDAV(
        this IEndpointRouteBuilder endpoints, string pattern = "/{*path}")
        {
            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<WebDAVMiddleware>()
                .Build();

            return endpoints.Map(pattern, pipeline).WithDisplayName("WebDAV");
        }
    }
}
