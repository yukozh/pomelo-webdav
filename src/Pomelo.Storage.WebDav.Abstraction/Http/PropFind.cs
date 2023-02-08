using System.Text;
using System.Threading;
using System.Web;
using Pomelo.Storage.WebDav.Abstractions.Lock;
using Pomelo.Storage.WebDav.Abstractions.Models;
using Pomelo.Storage.WebDav.Abstractions.Storage;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private static string GetBaseUrl(HttpContext context)
        {
            var baseUrlBuilder = new StringBuilder();
            baseUrlBuilder.Append(context.Request.Scheme);
            baseUrlBuilder.Append("://");
            baseUrlBuilder.Append(context.Request.Host.Value);
            if (context.Request.RouteValues["path"] != null)
            {
                var path = (context.Request.RouteValues["path"] as string).TrimEnd('/');
                var fullEndpoint = context.Request.Path.Value.TrimEnd('/');
                var baseEndpoint = fullEndpoint.Substring(0, fullEndpoint.Length - path.Length).TrimEnd('/');
                baseUrlBuilder.Append(baseEndpoint);
            }
            else
            {
                baseUrlBuilder.Append(context.Request.Path.Value.TrimEnd('/'));
            }
            var baseUrl = baseUrlBuilder.ToString();
            return baseUrl;
        }

        private static string GetUri(HttpContext context)
        {
            if (context.Request.RouteValues["path"] == null) 
            {
                return null;
            }

            return HttpUtility.UrlPathEncode(context.Request.RouteValues["path"].ToString().Trim('/'));
        }

        private async Task PropFindAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            var lockManager = context.RequestServices.GetRequiredService<IWebDAVLockManager>();
            var items = await storage.GetItemsAsync(
                context.GetRouteData().Values["path"] as string, 
                context.RequestAborted);

            var baseUrl = GetBaseUrl(context);
            string path = "";
            if (context.Request.RouteValues["path"] != null)
            {
                path = (context.Request.RouteValues["path"] as string).TrimEnd('/');
            }
            var isDirectory = string.IsNullOrEmpty(path) || await storage.IsDirectoryExistsAsync(path);
            if (isDirectory && (items == null || items.Count() == 0))
            {
                context.Response.StatusCode = 404;
                await context.Response.CompleteAsync();
                return;
            }

            var depth = context.Request.Headers.ContainsKey("Depth") 
                ? Convert.ToInt32(context.Request.Headers["Depth"]) 
                : 1;

            if (depth > 1)
            {
                context.Response.StatusCode = 400;
                await context.Response.CompleteAsync();
            }

            var response = "";
            if (isDirectory)
            {
                response = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <D:multistatus xmlns:D="DAV:">
                {await PropFindHelper.BuildSingleDirectoryAsync(items.First(x => x.Depth == 0), baseUrl, context.Request.Protocol, lockManager, context.RequestAborted)}
                {(depth == 0 ? "" : await PropFindHelper.BuildDirectoriesAsync(items.Where(x => x.Properties.ResourceType == ItemType.Directory && x.Depth > 0), baseUrl, context.Request.Protocol, lockManager, context.RequestAborted))}
                {(depth == 0 ? "" : await PropFindHelper.BuildFilesAsync(items.Where(x => x.Properties.ResourceType == ItemType.File), baseUrl, context.Request.Protocol, lockManager, context.RequestAborted))}
                </D:multistatus>
                """;
            }
            else 
            {
                var item = await storage.GetItemAsync(path, context.RequestAborted);
                if (item == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.CompleteAsync();
                    return;
                }
                response = await PropFindHelper.BuildSingleFileAsync(item, baseUrl, context.Request.Protocol, lockManager, context.RequestAborted);
            }

            context.Response.StatusCode = 207;
            context.Response.ContentType = "text/xml";
            context.Response.ContentLength = response.Length;
            await context.Response.WriteAsync(response, context.RequestAborted);
            await context.Response.CompleteAsync();
        }
    }

    file static class PropFindHelper
    {
        private static string ParseUrl(string url)
        {
            return url;
        }

        private static string ParseCreationTime(DateTime? time)
        {
            if (time == null)
            {
                time = DateTime.UtcNow;
            }

            return time.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
        }

        public static async Task<string> BuildSingleDirectoryAsync(
            Item item,
            string baseUrl, 
            string protocol,
            IWebDAVLockManager lockManager, 
            CancellationToken cancellationToken = default)
            => $"""
                <D:response>
                    <D:href>{ParseUrl($"{baseUrl}/{item.Href}")}</D:href>
                    <D:propstat>
                        <D:status>{protocol} 200 OK</D:status>
                        <D:prop>
                            <D:getlastmodified>{(item.Properties.LastModified ?? DateTime.UtcNow).ToString("r")}</D:getlastmodified>
                            <D:creationdate>{ParseCreationTime(item.Properties.CreationTime)}</D:creationdate>
                            <D:getcontentlength>{item.Properties.ContentLength}</D:getcontentlength>
                            <D:resourcetype>
                                <D:collection/>
                            </D:resourcetype>
                            <D:displayname/>
                            {await BuildLocksAsync(item.Href, lockManager, cancellationToken)}
                        </D:prop>
                    </D:propstat>
                </D:response>
             """;

        public static async Task<string> BuildLocksAsync(string uri, IWebDAVLockManager lockManager, CancellationToken cancellationToken = default)
        {
            uri = uri.Trim('/');
            var locks = await lockManager.GetLocksAsync(uri, cancellationToken);
            if (!locks.Any())
            {
                return "";
            }

            return $"""
                               <D:supportedlock>
                                   <D:lockentry>
                                       <D:lockscope>
                                           <D:{locks.First().Type.ToString().ToLowerInvariant()}/>
                                       </D:lockscope>
                                       <D:locktype>
                                           <D:write/>
                                       </D:locktype>
                                   </D:lockentry>
                               </D:supportedlock>
                """;
        }

        public static async Task<string> BuildDirectoriesAsync(
            IEnumerable<Item> items, 
            string baseUrl, 
            string protocol, 
            IWebDAVLockManager lockManager, 
            CancellationToken cancellationToken = default)
        { 
            var stringBuilder = new StringBuilder();

            foreach(var item in items)
            {
                stringBuilder.AppendLine(await BuildSingleDirectoryAsync(item, baseUrl, protocol, lockManager, cancellationToken));
            }

            return stringBuilder.ToString();
        }

        public static async Task<string> BuildSingleFileAsync(
            Item item, 
            string baseUrl,
            string protocol, 
            IWebDAVLockManager lockManager, 
            CancellationToken cancellationToken = default)
            => $"""
                    <D:response>
                        <D:href>{ParseUrl($"{baseUrl}/{item.Href}")}</D:href>
                        <D:propstat>
                            <D:status>{protocol} 200 OK</D:status>
                            <D:prop>
                                <D:resourcetype/>
                                <D:getcontentlength>{item.Properties.ContentLength}</D:getcontentlength>
                                <D:getetag>{item.Properties.Etag}</D:getetag>
                                <D:getcontenttype/>
                                <D:getlastmodified>{(item.Properties.LastModified ?? DateTime.UtcNow).ToString("r")}</D:getlastmodified>
                                <D:creationdate>{ParseCreationTime(item.Properties.CreationTime)}</D:creationdate>
                                <D:displayname/>
                                {await BuildLocksAsync(item.Href, lockManager, cancellationToken)}
                            </D:prop>
                        </D:propstat>
                    </D:response>
                 """;

        public static async Task<string> BuildFilesAsync(
            IEnumerable<Item> items, 
            string baseUrl, 
            string protocol,
            IWebDAVLockManager lockManager,
            CancellationToken cancellationToken = default)
        {
            var stringBuilder = new StringBuilder();

            foreach (var item in items)
            {
                stringBuilder.AppendLine(await BuildSingleFileAsync(item, baseUrl, protocol, lockManager, cancellationToken));
            }

            return stringBuilder.ToString();
        }
    }
}
