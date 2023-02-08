using Pomelo.Storage.WebDav.Abstractions.Lock;
using System.Xml.Linq;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task LockAsync(HttpContext context)
        {
            using var sr = new StreamReader(context.Request.Body);
            var content = await sr.ReadToEndAsync();
            XDocument doc = XDocument.Parse(content);
            var element = doc.Descendants("{DAV:}owner").Descendants("{DAV:}href").First();
            var owner = element.Value;

            var exclusive = doc.Descendants("{DAV:}exclusive");
            var lockType = exclusive == null 
                ? LockType.Shared 
                : LockType.Exclusive;

            var lockManager = context.RequestServices.GetRequiredService<IWebDAVLockManager>();
            try
            {
                var uri = GetUri(context);
                var _lock = await lockManager.LockAsync(
                    uri,
                    GetDepth(context),
                    lockType,
                    owner,
                    86400,
                    context.RequestAborted);

                context.Response.StatusCode = 200;
                var response = $"""
                    <?xml version="1.0" encoding="utf-8"?> 
                    <D:prop xmlns:D="DAV:"> 
                        <D:lockdiscovery> 
                            <D:activelock> 
                                <D:locktype><D:write/></D:locktype> 
                                <D:lockscope><D:{_lock.Type.ToString().ToLower()}/></D:lockscope> 
                                <D:depth>{(_lock.Depth == -1 ? "infinity" : _lock.Depth)}</D:depth> 
                                <D:owner> 
                                    <D:href>{_lock.Owner}</D:href> 
                                </D:owner> 
                                <D:timeout>Second-86400</D:timeout> 
                                <D:locktoken> 
                                    <D:href>urn:uuid:{_lock.LockToken}</D:href>
                                </D:locktoken>
                                <D:lockroot> 
                                    <D:href>{GetBaseUrl(context)}/{_lock.Uri}</D:href>
                                </D:lockroot> 
                            </D:activelock> 
                        </D:lockdiscovery> 
                    </D:prop>
                    """;
                context.Response.ContentType = "text/xml";
                context.Response.ContentLength = response.Length;
                await context.Response.WriteAsync(response);
                await context.Response.CompleteAsync();
            }
            catch(LockException) 
            {
                context.Response.StatusCode = 409;
                await context.Response.CompleteAsync();
            }
        }

        private static int GetDepth(HttpContext context)
        {
            if (!context.Request.Headers.ContainsKey("Depth"))
            {
                return 0;
            }

            var depth = context.Request.Headers["Depth"].ToString();
            if (depth.Equals("Infinity", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            return Convert.ToInt32(depth);
        }
    }
}
