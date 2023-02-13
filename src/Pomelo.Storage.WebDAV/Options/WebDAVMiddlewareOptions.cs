namespace Pomelo.Storage.WebDAV.Options
{
    public class WebDAVMiddlewareOptions
    {
        public string Pattern { get; set; } = "/{*path}";

        public bool AllowReadExclusiveLockedResources { get; set; }
    }
}
