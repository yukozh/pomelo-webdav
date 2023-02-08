using System.Text;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task PropFindAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            var items = await storage.GetItemsAsync(
                context.GetRouteData().Values["path"] as string, 
                context.RequestAborted);

            var baseUrlBuilder = new StringBuilder();
            baseUrlBuilder.Append(context.Request.Scheme);
            baseUrlBuilder.Append("://");
            baseUrlBuilder.Append(context.Request.Host.Value);
            string path = "";
            if (context.Request.RouteValues["path"] != null)
            {
                var length = (context.Request.RouteValues["path"] as string).Length;
                path = (context.Request.RouteValues["path"] as string).TrimEnd('/');
                var fullEndpoint = context.Request.Path.Value.TrimEnd('/');
                var baseEndpoint = fullEndpoint.Substring(0, fullEndpoint.Length - path.Length).TrimEnd('/');
                baseUrlBuilder.Append(baseEndpoint);
            }
            else 
            {
                baseUrlBuilder.Append(context.Request.Path.Value.TrimEnd('/'));
            }
            var baseUrl = baseUrlBuilder.ToString();
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
                {PropFindHelper.BuildSingleDirectory(items.First(x => x.Depth == 0), baseUrl, context.Request.Protocol)}
                {(depth == 0 ? "" : PropFindHelper.BuildDirectories(items.Where(x => x.Properties.ResourceType == ItemType.Directory && x.Depth > 0), baseUrl, context.Request.Protocol))}
                {(depth == 0 ? "" : PropFindHelper.BuildFiles(items.Where(x => x.Properties.ResourceType == ItemType.File), baseUrl, context.Request.Protocol))}
                </D:multistatus>
                """;
            }
            else 
            {
                var item = await storage.GetItemAsync(path, context.RequestAborted);
                if (item == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.CompleteAsync();
                    return;
                }
                response = PropFindHelper.BuildSingleFile(item, baseUrl, context.Request.Protocol);
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

        public static string BuildSingleDirectory(Item item, string baseUrl, string protocol)
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
                        </D:prop>
                    </D:propstat>
                </D:response>
             """;

        public static string BuildDirectories(IEnumerable<Item> items, string baseUrl, string protocol)
        { 
            var stringBuilder = new StringBuilder();

            foreach(var item in items)
            {
                stringBuilder.AppendLine(BuildSingleDirectory(item, baseUrl, protocol));
            }

            return stringBuilder.ToString();
        }

        public static string BuildSingleFile(Item item, string baseUrl, string protocol)
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
                            </D:prop>
                        </D:propstat>
                    </D:response>
                 """;

        public static string BuildFiles(IEnumerable<Item> items, string baseUrl, string protocol)
        {
            var stringBuilder = new StringBuilder();

            foreach (var item in items)
            {
                stringBuilder.AppendLine(BuildSingleFile(item, baseUrl, protocol));
            }

            return stringBuilder.ToString();
        }
    }
}
