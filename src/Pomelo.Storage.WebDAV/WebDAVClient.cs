using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pomelo.Storage.WebDAV
{
    public class WebDAVClient : HttpClient
    {
        public WebDAVClient()
        { }

        public WebDAVClient(HttpMessageHandler httpMessageHandler)
            : base(httpMessageHandler)
        { }

        public WebDAVClient(
            HttpMessageHandler httpMessageHandler, 
            bool disposeHandler)
            : base(httpMessageHandler, disposeHandler)
        { }

        #region PROPFIND
        public Task<HttpResponseMessage> PropFindAllPropertiesAsync(
            string uri,
            int depth = 0,
            CancellationToken cancellationToken = default)
            => PropFindAsync(uri, depth, "allprop", null, cancellationToken);

        public Task<HttpResponseMessage> PropFindAsync(
            string uri,
            int depth = 0,
            IReadOnlyCollection<Models.Property> properties = null,
            CancellationToken cancellationToken = default)
            => PropFindAsync(uri, depth, null, properties, cancellationToken);

        private async Task<HttpResponseMessage> PropFindAsync(
            string uri,
            int depth,
            string operation,
            IReadOnlyCollection<Models.Property> properties,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("PROPFIND"), uri);
            message.Headers.Add("Depth", depth == -1 ? "infinity" : depth.ToString());
            if (operation != null)
            {
                var request = $"""
                    <?xml version="1.0" encoding="utf-8" ?> 
                    <propfind xmlns="DAV:"> 
                        <{operation}/>
                    </propfind>
                    """;
                message.Content = new StringContent(request, Encoding.UTF8, "application/xml");
            }
            else if (properties != null && properties.Any())
            {
                var namespaceIndex = 0;
                var namespaces = properties
                    .GroupBy(x => x.Namespace)
                    .Select(x => new { Namespace = x.Key, Names = x.Select(x => x.Name) });

                var nsMap = new List<(string shorted, string full)>();
                var propertyPairs = new List<(string shortedNamespace, string name)>();
                foreach(var ns in namespaces)
                {
                    var key = $"ns{namespaceIndex++}";
                    nsMap.Add((key, ns.Namespace));
                    foreach(var name in ns.Names)
                    {
                        propertyPairs.Add((key, name));
                    }
                }

                var request = $"""
                    <?xml version="1.0" encoding="utf-8" ?> 
                    <D:propfind xmlns:D="DAV:"> 
                        <D:prop {BuildNamespacesString(nsMap)}> 
                            {BuildPropertiesWithNamespace(propertyPairs)}
                        </D:prop> 
                    </D:propfind>
                    """;
                message.Content = new StringContent(request, Encoding.UTF8, "application/xml");
            }

            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region OPTIONS
        public async Task<HttpResponseMessage> OptionsAsync(
            string uri,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("OPTIONS"), uri);
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region Misc
        protected virtual string BuildPropertiesWithNamespace(
            IEnumerable<(string shortedNamespace, string name)> properties)
        { 
            if (!properties.Any())
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach(var property in properties)
            {
                builder.AppendLine($"""
                    <{property.shortedNamespace}:{property.name}/>
                    """);
            }
            return builder.ToString();
        }

        protected virtual string BuildNamespacesString(IEnumerable<(string shorted, string full)> namespaces)
        { 
            if (!namespaces.Any())
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach(var ns in namespaces)
            {
                builder.Append(@$"xmlns:{ns.shorted}=""{ns.full}"" ");
            }

            return builder.ToString();
        }
        #endregion
    }
}
