using Pomelo.Storage.WebDAV.Http;

namespace Pomelo.Storage.WebDAV.Utils
{
    public interface IRelativeUrlHelper
    {
        string RemoveBaseUrl(WebDAVContext context, string encodedUrl);

        string GetEncodedBaseUrl(WebDAVContext context);
    }
}
