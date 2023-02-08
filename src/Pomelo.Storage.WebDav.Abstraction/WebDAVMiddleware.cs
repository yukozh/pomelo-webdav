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
            var method = httpContext.Request.Method.ToUpper();
            if (!AllowedMethods.Contains(method))
            {
                httpContext.Response.StatusCode = 405;
                await httpContext.Response.CompleteAsync();
                return;
            }

            httpContext.Response.Headers.Server = "Pomelo WebDAV Abstractions";
            var lockManager = httpContext.RequestServices.GetRequiredService<IWebDAVLockManager>();

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
                case "MKCOL":
                    if (await IsAbleToWriteAsync(httpContext))
                    {
                        await MkcolAsync(httpContext);
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
                    if (await IsAbleToWriteAsync(httpContext))
                    {
                        await PropPatchAsync(httpContext);
                    }
                    return;
                case "LOCK":
                    await LockAsync(httpContext);
                    return;
                case "UNLOCK":
                    await UnlockAsync(httpContext);
                    return;
                default:
                    await _next(httpContext);
                    return;
            }
        }

        private Dictionary<int, string> StatusCodeMapping = new Dictionary<int, string>
        {
            [100] = "Continue",
            [101] = "Switching Protocols",
            [200] = "OK",
            [201] = "Created",
            [202] = "Accepted",
            [203] = "Non-Authoritative Information",
            [204] = "No Content",
            [205] = "Reset Content",
            [206] = "Partial Content",
            [207] = "Multi-Status",
            [300] = "Multiple Choices",
            [301] = "Moved Permanently",
            [302] = "Found",
            [303] = "See Other",
            [304] = "Not Modified",
            [305] = "Use Proxy",
            [307] = "Temporary Redirect",
            [400] = "Bad Request",
            [401] = "Unauthorized",
            [402] = "Payment Required",
            [403] = "Forbidden",
            [404] = "Not Found",
            [405] = "Method Not Allowed",
            [406] = "Not Acceptable",
            [407] = "Proxy Authentication Required",
            [408] = "Request Time-out",
            [409] = "Conflict",
            [410] = "Gone",
            [411] = "Length Required",
            [412] = "Precondition Failed",
            [413] = "Request Entity Too Large",
            [414] = "Request-URI Too Large",
            [415] = "Unsupported Media Type",
            [416] = "Requested range not satisfiable",
            [417] = "Expectation Failed",
            [422] = "Unprocessable Entity",
            [424] = "Failed Dependency",
            [500] = "Internal Server Error",
            [501] = "Not Implemented",
            [502] = "Bad Gateway",
            [503] = "Service Unavailable",
            [504] = "Gateway Time-out"
        };

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
            var lockManager = context.RequestServices.GetRequiredService<IWebDAVLockManager>();
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
            var lockManager = context.RequestServices.GetRequiredService<IWebDAVLockManager>();
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
            var lockManager = context.RequestServices.GetRequiredService<IWebDAVLockManager>();
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

            return path.Substring(0, index).Trim('/');
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
