using Pomelo.Storage.WebDav.Abstractions.Lock;
using Pomelo.Storage.WebDav.Abstractions.Storage;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task DeleteAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            var lockManager = context.RequestServices.GetRequiredService<IWebDAVLockManager>();
            await storage.DeleteItemAsync(context.Request.RouteValues["path"] as string, context.RequestAborted);
            await lockManager.DeleteLockByUriAsync(context.Request.RouteValues["path"] as string, context.RequestAborted);
            await context.Response.CompleteAsync();
        }
    }
}
