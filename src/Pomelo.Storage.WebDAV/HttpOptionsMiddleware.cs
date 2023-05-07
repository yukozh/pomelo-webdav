using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Pomelo.Storage.WebDAV
{
    public class HttpOptionsMethodMiddleware
    {
        private readonly RequestDelegate _next;

        public HttpOptionsMethodMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private readonly static string[] AllowedMethods = new[]
        {
            "OPTIONS",
            "GET",
            "HEAD",
            "PUT",
            "POST",
            "PATCH",
            "COPY",
            "PROPFIND",
            "PROPPATCH",
            "MKCOL",
            "DELETE",
            "MOVE",
            "LOCK",
            "UNLOCK"
        };

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Method.ToUpper() == "OPTIONS")
            {
                httpContext.Response.Headers.Add("Allow", AllowedMethods);
                httpContext.Response.Headers.Add("Public", AllowedMethods);
                httpContext.Response.Headers.Add("Dav", "1, 2, 3");
                await httpContext.Response.CompleteAsync();
                return;
            }

            await _next(httpContext);
        }
    }

    public static class OptionsMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpOptionsMethodMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpOptionsMethodMiddleware>();
        }
    }
}
