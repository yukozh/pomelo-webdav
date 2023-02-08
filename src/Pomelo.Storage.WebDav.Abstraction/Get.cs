namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task GetAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            if (!await storage.IsFileExistsAsync(context.Request.RouteValues["path"] as string, context.RequestAborted))
            {
                context.Response.StatusCode = 404;
                await context.Response.CompleteAsync();
                return;
            }

            using var fs = await storage.GetFileReadStreamAsync(context.Request.RouteValues["path"] as string, context.RequestAborted);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/octet-stream";
            await fs.CopyToAsync(context.Response.Body, context.RequestAborted);
            await context.Response.CompleteAsync();
        }
    }
}
