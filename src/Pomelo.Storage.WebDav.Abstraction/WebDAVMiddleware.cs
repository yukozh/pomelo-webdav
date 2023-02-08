using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pomelo.Storage.WebDav.Abstractions.Lock;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Pomelo.Storage.WebDav.Abstractions
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public partial class WebDAVMiddleware
    {
        private readonly RequestDelegate _next;

        public WebDAVMiddleware(RequestDelegate next)
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
            "Copy",
            "PROPFIND",
            "PROPPATCH",
            "ACL",
            "MKCOL",
            "DELETE",
            "MOVE",
            "LOCK",
            "UNLOCK"
        };

        public async Task Invoke(HttpContext httpContext)
        {
            var method = httpContext.Request.Method.ToUpper();
            if (!AllowedMethods.Contains(method))
            {
                httpContext.Response.StatusCode = 405;
                await httpContext.Response.CompleteAsync();
                return;
            }

            httpContext.Response.Headers.Server = "Pomelo WebDAV Abstractions";
            var lockManager = httpContext.RequestServices.GetRequiredService<IWebDavLockManager>();

            switch (method)
            {
                case "OPTIONS":
                    await OptionsAsync(httpContext);
                    return;
                case "HEAD":
                    await HeadAsync(httpContext);
                    break;
                case "GET":
                    if (await IsAbleToReadAsync(httpContext))
                    {
                        await GetAsync(httpContext);
                    }
                    return;
                case "PUT":
                case "POST":
                    if (await IsAbleToWriteAsync(httpContext))
                    {
                        await PutAsync(httpContext);
                    }
                    return;
                case "DELETE":
                    if (await IsAbleToWriteAsync(httpContext))
                    {
                        await DeleteAsync(httpContext);
                    }
                    break;
                case "PROPFIND":
                    await PropFindAsync(httpContext);
                    return;
                case "MOVE":
                    if (await IsAbleToMoveOrCopyAsync(httpContext))
                    {
                        await MoveAsync(httpContext);
                    }
                    return;
                case "COPY":
                    if (await IsAbleToMoveOrCopyAsync(httpContext))
                    {
                        await CopyAsync(httpContext);
                    }
                    return;
                case "PROPPATCH":
                    return;
                case "ACL":
                    return;
                case "LOCK":
                    await LockAsync(httpContext);
                    return;
                case "UNLOCK":
                    await lockManager.UnlockAsync(Guid.Parse(httpContext.Request.Headers["Lock-Token"].ToString()));
                    httpContext.Response.StatusCode = 204;
                    await httpContext.Response.CompleteAsync();
                    return;
                default:
                    await _next(httpContext);
                    return;
            }
        }

        private static async Task<bool> IsAbleToMoveOrCopyAsync(HttpContext context)
        {
            if (!await IsAbleToWriteAsync(context))
            {
                return false;
            }

            return await IsAbleToWriteAsync(context, context.Request.Headers["Destination"].ToString().Substring(GetBaseUrl(context).Length).Trim('/'));
        }

        private static async Task<bool> IsAbleToWriteAsync(HttpContext context, string uri)
        {
            var lockManager = context.RequestServices.GetRequiredService<IWebDavLockManager>();
            var locks = (await lockManager.GetLocksAsync(uri, context.RequestAborted));
            var lockTokens = GetLockTokens(context);
            if (locks.Any(x => !lockTokens.Contains(x.LockToken) && x.Type == LockType.Exclusive))
            {
                context.Response.StatusCode = 423;
                await context.Response.CompleteAsync();
                return false;
            }
            else
            {
                return true;
            }
        }

        private static async Task<bool> IsAbleToReadAsync(HttpContext context)
        {
            var lockManager = context.RequestServices.GetRequiredService<IWebDavLockManager>();
            var uri = GetUri(context);
            var locks = (await lockManager.GetLocksAsync(uri, context.RequestAborted));
            var lockTokens = GetLockTokens(context);
            if (locks.Any(x => !lockTokens.Contains(x.LockToken) && x.Type == LockType.Exclusive))
            {
                context.Response.StatusCode = 423;
                await context.Response.CompleteAsync();
                return false;
            }
            else
            {
                return true;
            }
        }

        private static async Task<bool> IsAbleToWriteAsync(HttpContext context)
        {
            var lockManager = context.RequestServices.GetRequiredService<IWebDavLockManager>();
            var uri = GetUri(context);
            var locks = (await lockManager.GetLocksAsync(uri, context.RequestAborted));
            var lockTokens = GetLockTokens(context);
            if (locks.Any(x => !lockTokens.Contains(x.LockToken) && x.Type == LockType.Exclusive) 
                || locks.Any(x => x.Type == LockType.Shared))
            {
                context.Response.StatusCode = 423;
                await context.Response.CompleteAsync();
                return false;
            }
            else
            {
                return true;
            }
        }

        private static string GetUpUrl(string path)
        {
            var index = path.LastIndexOf('/');
            if (index < 1)
            {
                return "";
            }

            return path.Substring(0, index - 1);
        }

        private static Regex urnRegex = new Regex("(?<=<urn:uuid:)[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}(?=>)");

        private static IEnumerable<Guid> GetLockTokens(HttpContext context)
        {
            var _if = context.Request.Headers["If"].ToString();
            if (string.IsNullOrEmpty(_if))
            {
                return new Guid[] { };
            }

            return urnRegex.Matches(_if).Cast<Match>().Select(x => Guid.Parse(x.Value));
        }
    }

    public static class WebDAVMiddlewareExtensions
    {
        public static IEndpointConventionBuilder MapWebDAV(
        this IEndpointRouteBuilder endpoints, string pattern = "/{*path}")
        {
            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<WebDAVMiddleware>()
                .Build();

            return endpoints.Map(pattern, pipeline).WithDisplayName("WebDAV");
        }
    }
}
