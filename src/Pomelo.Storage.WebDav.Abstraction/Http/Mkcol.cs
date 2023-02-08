using Pomelo.Storage.WebDav.Abstractions.Storage;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task MkcolAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            if (await storage.IsDirectoryExistsAsync(context.Request.RouteValues["path"] as string, context.RequestAborted))
            {
                context.Response.StatusCode = 409;
                await context.Response.CompleteAsync();
                return;
            }

            await storage.CreateDirectoryAsync(context.Request.RouteValues["path"] as string, context.RequestAborted);
            context.Response.StatusCode = 201;
            await context.Response.CompleteAsync();
        }
    }
}
