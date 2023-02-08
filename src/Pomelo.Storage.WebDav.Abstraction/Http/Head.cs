using Pomelo.Storage.WebDav.Abstractions.Storage;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task HeadAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            if (!await storage.IsFileExistsAsync(context.Request.RouteValues["path"] as string, context.RequestAborted))
            {
                context.Response.StatusCode = 404;
                await context.Response.CompleteAsync();
                return;
            }

            var info = await storage.GetItemAsync(context.Request.RouteValues["path"] as string, context.RequestAborted);
            if (info == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.CompleteAsync();
                return;
            }

            context.Response.Headers["Accept-Ranges"] = "bytes";
            context.Response.Headers["Etag"] = info.Properties.Etag;
            context.Response.Headers["Last-Modified"] = (info.Properties.LastModified ?? DateTime.UtcNow).ToString("r");
            context.Response.Headers.ContentLength = info.Properties.ContentLength;
            await context.Response.CompleteAsync();
        }
    }
}
