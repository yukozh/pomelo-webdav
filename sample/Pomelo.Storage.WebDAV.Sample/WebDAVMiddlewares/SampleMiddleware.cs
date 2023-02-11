// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Pomelo.Storage.WebDAV.Http;
using Pomelo.Storage.WebDAV.Middleware;

namespace Pomelo.Storage.WebDAV.Sample.WebDAVMiddlewares
{
    public class SampleMiddleware : IWebDAVMiddleware
    {
        public async Task Invoke(WebDAVContext context, WebDAVMiddlewareRequestDelegate next)
        {
            context.HttpContext.Response.Headers["Server"] = "Pomelo WebDAV Sample Server";
            await next(context);
        }
    }

    public static class SampleMiddlewareExtensions
    {
        public static IServiceCollection AddSampleMiddleware(this IServiceCollection services)
            => services.AddSingleton<IWebDAVMiddleware, SampleMiddleware>();
    }
        
}
