using System.Text;
using System.Xml.Linq;
using Pomelo.Storage.WebDav.Abstractions.Models;
using Pomelo.Storage.WebDav.Abstractions.Storage;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private static readonly IReadOnlyList<XElement> EmptyXElementList = new List<XElement>();

        private async Task PropPatchAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            if (!await storage.IsFileExistsAsync(context.Request.RouteValues["path"] as string, context.RequestAborted))
            {
                context.Response.StatusCode = 404;
                await context.Response.CompleteAsync();
                return;
            }

            using var sr = new StreamReader(context.Request.Body);
            var doc = XDocument.Parse(await sr.ReadToEndAsync());
            var set = doc.Descendants("{DAV:}set").FirstOrDefault();
            var remove = doc.Descendants("{DAV:}remove").FirstOrDefault();
            var elementsToSet = set != null 
                ? set.Descendants().ToList() 
                : EmptyXElementList;
            var elementsToRemove = remove != null 
                ? remove.Descendants().ToList() 
                : EmptyXElementList;
            var results = await storage.PatchPropertyAsync(
                context.Request.RouteValues["path"] as string, 
                elementsToSet, 
                elementsToRemove, 
                context.RequestAborted);

            context.Response.StatusCode = 207;
            var response = $"""
                <?xml version="1.0" encoding="utf-8" ?> 
                <D:multistatus xmlns:D="DAV:" xmlns:Z="http://ns.example.com/standards/z39.50/"> 
                    <D:response> 
                        <D:href>http://www.example.com/bar.html</D:href> 
                        {BuildPropStat(results, context.Request.Protocol)}
                    </D:response> 
                </D:multistatus> 
                """;
            context.Response.ContentLength = response.Length;
            await context.Response.WriteAsync(response, context.RequestAborted);
            await context.Response.CompleteAsync();
        }

        private string BuildPropStat(IEnumerable<PatchPropertyResult> results, string protocol)
        {
            var builder = new StringBuilder();
            foreach (var result in results) 
            {
                builder.AppendLine(BuildSinglePropStat(result, protocol));
            }
            return builder.ToString();
        }

        private string BuildSinglePropStat(PatchPropertyResult result, string protocol)
        { 
            return $"""
                        <D:propstat> 
                            <D:prop><{result.PropertyXName}/></D:prop> 
                            <D:status>{protocol} {result.StatusCode} {(StatusCodeMapping.ContainsKey(result.StatusCode) ? StatusCodeMapping[result.StatusCode] : "Unknown")}</D:status> 
                        </D:propstat>
                """;
        }
    }
}
