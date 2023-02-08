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

            switch (method)
            {
                case "OPTIONS":
                    await OptionsAsync(httpContext);
                    return;
                //case "HEAD":
                //    await HeadAsync(httpContext);
                //    break;
                case "GET":
                    await GetAsync(httpContext);
                    return;
                case "PUT":
                case "POST":
                    await PutAsync(httpContext);
                    return;
                case "DELETE":
                    await DeleteAsync(httpContext);
                    break;
                case "PROPFIND":
                    await PropFindAsync(httpContext);
                    return;
                case "MOVE":
                    await MoveAsync(httpContext);
                    return;
                case "COPY":
                    await CopyAsync(httpContext);
                    return;
                case "PROPPATCH":
                    return;
                case "ACL":
                    return;
                case "LOCK":
                    return;
                case "UNLOCK":
                    return;
                default:
                    await _next(httpContext);
                    return;
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
