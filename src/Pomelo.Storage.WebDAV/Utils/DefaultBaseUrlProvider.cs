using Microsoft.Extensions.DependencyInjection;
using Pomelo.Storage.WebDAV.Http;
using System.Linq;
using System.Text;
using System.Web;

namespace Pomelo.Storage.WebDAV.Utils
{
    public class DefaultRelativeUrlHelper : IRelativeUrlHelper
    {
        public virtual string GetEncodedBaseUrl(WebDAVContext context)
        {
            if (context.HttpContext.Request.Headers.ContainsKey("X-Forwarded-WebDAV-BaseUrl"))
            {
                return context.HttpContext.Request.Headers["X-Forwarded-WebDAV-BaseUrl"].First().TrimEnd('/');
            }

            var baseUrlBuilder = new StringBuilder();
            baseUrlBuilder.Append(context.HttpContext.Request.Scheme);
            baseUrlBuilder.Append("://");
            baseUrlBuilder.Append(context.HttpContext.Request.Host.Value);
            if (context.HttpContext.Request.RouteValues["path"] != null)
            {
                var path = (context.HttpContext.Request.RouteValues["path"] as string).TrimEnd('/');
                var fullEndpoint = context.HttpContext.Request.Path.Value.TrimEnd('/');
                var baseEndpoint = fullEndpoint.Substring(0, fullEndpoint.Length - path.Length).TrimEnd('/');
                baseUrlBuilder.Append(baseEndpoint);
            }
            else
            {
                baseUrlBuilder.Append(context.HttpContext.Request.Path.Value.TrimEnd('/'));
            }
            var baseUrl = baseUrlBuilder.ToString();
            return HttpUtility.UrlPathEncode(baseUrl.TrimEnd('/'));
        }

        public virtual string RemoveBaseUrl(WebDAVContext context, string encodedUrl)
        {
            return encodedUrl.Substring(GetEncodedBaseUrl(context).Length).Trim('/');
        }
    }

    public static class DefaultRelativeUrlHelperExtensions
    {
        public static IServiceCollection AddDefaultRelativeUrlHelper(this IServiceCollection services)
            => services.AddSingleton<IRelativeUrlHelper, DefaultRelativeUrlHelper>();
    }
}
