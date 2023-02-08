namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task MoveAsync(HttpContext context)
        {
            var storage = context.RequestServices.GetRequiredService<IWebDAVStorageProvider>();
            if (!await storage.IsFileExistsAsync(context.Request.RouteValues["path"] as string, context.RequestAborted))
            {
                context.Response.StatusCode = 404;
                await context.Response.CompleteAsync();
                return;
            }

            var dest = context.Request.Headers["Destination"].ToString().Substring(GetBaseUrl(context).Length).Trim('/');
            await storage.MoveItemAsync(
                context.Request.RouteValues["path"] as string, 
                dest,
                context.Request.Headers.ContainsKey("Overwrite")
                    ? context.Request.Headers["Overwrite"].ToString().ToUpper() == "T"
                    : true, 
                context.RequestAborted);
            context.Response.StatusCode = 201;
            context.Response.Headers["Location"] = context.Request.Headers["Destination"].ToString();
            await context.Response.CompleteAsync();
        }
    }
}
