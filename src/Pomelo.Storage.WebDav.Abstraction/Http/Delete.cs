using Pomelo.Storage.WebDav.Abstractions.Storage;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task DeleteAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            await storage.DeleteItemAsync(context.Request.RouteValues["path"] as string, context.RequestAborted);
            await context.Response.CompleteAsync();
        }
    }
}
