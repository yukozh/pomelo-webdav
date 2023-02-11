// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Pomelo.Storage.WebDAV.Http;
using Pomelo.Storage.WebDAV.Middleware;

namespace Pomelo.Storage.WebDAV.Sample.WebDAVMiddlewares
{
    public class BasicAuthMiddleware : IWebDAVMiddleware
    {
        public async Task Invoke(WebDAVContext context, WebDAVMiddlewareRequestDelegate next)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                context.HttpContext.Response.StatusCode = 401;
                await context.HttpContext.Response.CompleteAsync();
                return;
            }

            await next(context);
        }
    }

    public static class BasicAuthMiddlewareExtensions
    {
        public static IServiceCollection AddBasicAuthMiddleware(this IServiceCollection services)
            => services.AddSingleton<IWebDAVMiddleware, BasicAuthMiddleware>();
    }
        
}
