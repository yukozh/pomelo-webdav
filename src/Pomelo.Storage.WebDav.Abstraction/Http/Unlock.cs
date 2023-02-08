using Pomelo.Storage.WebDav.Abstractions.Lock;

namespace Pomelo.Storage.WebDav.Abstractions
{
    public partial class WebDAVMiddleware
    {
        private async Task UnlockAsync(HttpContext context)
        {
            var lockManager = context.RequestServices.GetRequiredService<IWebDavLockManager>();
            var lockToken = context.Request.Headers["Lock-Token"].ToString().TrimStart('<').TrimEnd('>');
            var lockTokenGuid = lockToken.Substring("urn:uuid:".Length);
            await lockManager.UnlockAsync(Guid.Parse(lockTokenGuid));
            context.Response.StatusCode = 204;
            await context.Response.CompleteAsync();
        }
    }
}
