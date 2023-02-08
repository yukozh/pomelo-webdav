using Pomelo.Storage.WebDav.Abstractions.Storage;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task PutAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            if (!await storage.IsDirectoryExistsAsync(GetUpUrl(context.Request.RouteValues["path"] as string), context.RequestAborted))
            {
                context.Response.StatusCode = 404;
                await context.Response.CompleteAsync();
                return;
            }

            using var fs = await storage.GetFileWriteStreamAsync(context.Request.RouteValues["path"] as string, context.RequestAborted);
            await context.Request.Body.CopyToAsync(fs, context.RequestAborted);

            context.Response.StatusCode = 204;
            await context.Response.CompleteAsync();
        }
    }
}
