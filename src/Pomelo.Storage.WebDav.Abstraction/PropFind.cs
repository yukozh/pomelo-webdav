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
            if (context.Request.RouteValues["path"] != null)
            {
                var length = (context.Request.RouteValues["path"] as string).Length;
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

            if (items == null || items.Count() == 0)
            {
                context.Response.StatusCode = 207;
                var notFoundResponse = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <D:multistatus xmlns:D="DAV:">
                   <D:response>
                       <D:href>{baseUrl}</D:href>
                       <D:propstat>
                           <D:status>{context.Request.Protocol} 404 Not Found</D:status>
                       </D:propstat>
                   </D:response>
                </D:multistatus>
                """;
                context.Response.ContentLength = notFoundResponse.Length;
                await context.Response.WriteAsync(notFoundResponse);
                await context.Response.CompleteAsync();
                return;
            }

            var baseDirectory = items.SingleOrDefault(x => x.Properties.ResourceType == ItemType.RootDirectory);
            if (baseDirectory == null)
            {
                throw new InvalidDataException("Missing root directory");
            }

            var depth = context.Request.Headers.ContainsKey("Depth") 
                ? Convert.ToInt32(context.Request.Headers["Depth"]) 
                : 1;

            var response = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <D:multistatus xmlns:D="DAV:">
                {PropFindHelper.BuildBaseDirectory(baseDirectory, baseUrl, context.Request.Protocol)}
                {(depth == 0 ? "" : PropFindHelper.BuildDirectories(items.Where(x => x.Properties.ResourceType == ItemType.Directory), baseUrl, context.Request.Protocol))}
                {(depth == 0 ? "" : PropFindHelper.BuildFiles(items.Where(x => x.Properties.ResourceType == ItemType.File), baseUrl, context.Request.Protocol))}
                </D:multistatus>
                """;

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

        public static string BuildBaseDirectory(Item item, string baseUrl, string protocol)
        {
            return $"""
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
                        </D:prop>
                    </D:propstat>
                </D:response>
             """;
        }

        public static string BuildDirectories(IEnumerable<Item> items, string baseUrl, string protocol)
        { 
            var stringBuilder = new StringBuilder();

            foreach(var item in items)
            {
                stringBuilder.AppendLine($"""
                    <D:response>
                        <D:href>{ParseUrl($"{baseUrl}/{item.Href}")}</D:href>
                        <D:propstat>
                            <D:status>{protocol} 200 OK</D:status>
                            <D:prop>
                                <D:resourcetype>
                                    <D:collection/>
                                </D:resourcetype>
                                <D:getlastmodified>{(item.Properties.LastModified ?? DateTime.UtcNow).ToString("r")}</D:getlastmodified>
                                <D:creationdate>{ParseCreationTime(item.Properties.CreationTime)}</D:creationdate>
                                <D:getcontentlength>{item.Properties.ContentLength}</D:getcontentlength>
                                <D:displayname/>
                            </D:prop>
                        </D:propstat>
                    </D:response>
                 """);
            }

            return stringBuilder.ToString();
        }

        public static string BuildFiles(IEnumerable<Item> items, string baseUrl, string protocol)
        {
            var stringBuilder = new StringBuilder();

            foreach (var item in items)
            {
                stringBuilder.AppendLine($"""
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
                 """);
            }

            return stringBuilder.ToString();
        }
    }
}
