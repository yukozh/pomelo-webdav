using System.Threading.Tasks;
using Pomelo.Storage.WebDAV.Http;

namespace Pomelo.Storage.WebDAV.Middleware
{
    public interface IWebDAVMiddleware
    {
        Task Invoke(WebDAVContext context, WebDAVMiddlewareRequestDelegate next);
    }
}
