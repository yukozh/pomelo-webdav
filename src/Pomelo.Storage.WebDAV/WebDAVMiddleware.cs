// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.Storage.WebDAV.Factory;
using Pomelo.Storage.WebDAV.Http;
using Pomelo.Storage.WebDAV.Lock;
using Pomelo.Storage.WebDAV.Middleware;
using Pomelo.Storage.WebDAV.Options;

namespace Pomelo.Storage.WebDAV
{
    public delegate Task WebDAVMiddlewareRequestDelegate(WebDAVContext context);

    public partial class WebDAVMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebDAVMiddlewareOptions _options;

        public WebDAVMiddleware(RequestDelegate next, WebDAVMiddlewareOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var method = httpContext.Request.Method.ToUpper();
            httpContext.Response.Headers["Server"] = "Pomelo WebDAV Server";

            // Execute Middlewares
            var webDAVcontext = new WebDAVContext(httpContext);
            var middlewares = httpContext.RequestServices.GetServices<IWebDAVMiddleware>();
            foreach(var middleware in middlewares)
            {
                var executed = true;
                WebDAVMiddlewareRequestDelegate action = (context) =>
                {
                    executed = false;
                    return Task.CompletedTask;
                };
                await middleware.Invoke(webDAVcontext, action);
                if (executed)
                {
                    return;
                }
            }

            // Default behavior
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
                    if (_options.AllowReadExclusiveLockedResources 
                        || await handler.IsAbleToReadAsync())
                    {
                        await handler.GetAsync();
                    }
                    return;
                case "PUT":
                case "POST":
                case "PATCH":
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
            this IEndpointRouteBuilder endpoints,
            Action<WebDAVMiddlewareOptions> configureOptions = null)
        {
            var options = new WebDAVMiddlewareOptions();
            configureOptions?.Invoke(options);

            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<WebDAVMiddleware>(options)
                .Build();

            return endpoints.Map(options.Pattern, pipeline).WithDisplayName("WebDAV");
        }
    }
}
