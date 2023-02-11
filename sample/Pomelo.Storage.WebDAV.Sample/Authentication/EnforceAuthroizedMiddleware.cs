// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

namespace Pomelo.Storage.WebDAV.Sample.Authentication
{
    public class EnforceAuthroizedMiddleware
    {
        private readonly RequestDelegate _next;

        public EnforceAuthroizedMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.CompleteAsync();
                return;
            }

            await _next(httpContext);
        }
    }

    public static class EnforceAuthroizedMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnforceAuthroizedMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnforceAuthroizedMiddleware>();
        }
    }
}
