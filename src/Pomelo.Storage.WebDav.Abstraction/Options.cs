namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        public async Task OptionsAsync(HttpContext context)
        {
            context.Response.Headers.Allow = string.Join(", ", AllowedMethods);
            context.Response.Headers["Public"] = string.Join(", ", AllowedMethods);
            context.Response.Headers["DAV"] = "1, 2, 3";
            await context.Response.CompleteAsync();
        }
    }
}
