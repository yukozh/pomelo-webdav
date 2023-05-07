// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Pomelo.Storage.WebDAV.Lock;

namespace Pomelo.Storage.WebDAV
{
    public class WebDAVHttpClient : HttpClient
    {
        public WebDAVHttpClient()
        { }

        public WebDAVHttpClient(HttpMessageHandler httpMessageHandler)
            : base(httpMessageHandler)
        { }

        public WebDAVHttpClient(
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

        #region HEAD
        public async Task<HttpResponseMessage> HeadAsync(
            string uri,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("HEAD"), uri);
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region PROPPATCH
        public async Task<HttpResponseMessage> PropPatchAsync(
            string uri,
            IEnumerable<XElement> setPropertyValues = null,
            IEnumerable<XElement> removeProperties = null,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("PROPPATCH"), uri);
            var request = $"""
                <?xml version="1.0" encoding="utf-8" ?> 
                <D:propertyupdate xmlns:D="DAV:">
                    <D:set> 
                        <D:prop> 
                            {string.Join("\r\n", setPropertyValues.Select(x => x.ToString()))}
                        </D:prop> 
                    </D:set> 
                    <D:remove> 
                        <D:prop>
                            {string.Join("\r\n", removeProperties.Select(x => x.ToString()))}
                        </D:prop> 
                    </D:remove> 
                </D:propertyupdate> 
                """;
            message.Content = new StringContent(request, Encoding.UTF8, "application/xml");
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region LOCK
        public async Task<HttpResponseMessage> LockAsync(
            string uri,
            LockType type,
            long timeoutSeconds = 3600,
            int depth = 0,
            string owner = null,
            string refreshToken = null,
            CancellationToken cancellationToken = default)
        {
            if (owner == null)
            {
                if (!string.IsNullOrEmpty(Environment.UserDomainName))
                {
                    owner = Environment.UserDomainName + "/" + Environment.UserName;
                }
                else
                {
                    owner = Environment.MachineName + "/" + Environment.UserName;
                }
            }

            using var message = new HttpRequestMessage(new HttpMethod("LOCK"), uri);
            message.Headers.Add("Depth", depth == -1 ? "infinity" : depth.ToString());
            message.Headers.Add("Timeout", timeoutSeconds == -1 ? "Infinite" : "Second-" + timeoutSeconds.ToString());
            if (refreshToken != null)
            {
                message.Headers.Add("If", $"(<{refreshToken}>)");
            }
            else
            {
                var request = $"""
                <D:lockinfo xmlns:D='DAV:'> 
                    <D:lockscope><D:{type.ToString().ToLower()}/></D:lockscope> 
                    <D:locktype><D:write/></D:locktype> 
                    <D:owner> 
                        <D:href>{SecurityElement.Escape(owner)}</D:href> 
                    </D:owner> 
                </D:lockinfo> 
                """;
                message.Content = new StringContent(request, Encoding.UTF8, "application/xml");
            }
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region UNLOCK
        public async Task<HttpResponseMessage> UnlockAsync(
            string uri,
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("UNLOCK"), uri);
            message.Headers.Add("Lock-Token", $"<{refreshToken}>");
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region MKCOL
        public async Task<HttpResponseMessage> MkcolAsync(
            string uri,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("MKCOL"), uri);
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region GET
        public async Task<HttpResponseMessage> GetRangeAsync(
            string uri,
            System.Net.Http.Headers.RangeHeaderValue range,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("GET"), uri);
            message.Headers.Range = range;
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region PUT
        public async Task<HttpResponseMessage> PutRangeAsync(
            string uri,
            Stream stream,
            System.Net.Http.Headers.RangeHeaderValue range,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("PUT"), uri);
            message.Headers.Range = range;
            message.Content = new StreamContent(stream);
            return await SendAsync(message, cancellationToken);
        }
        #endregion

        #region Move
        public async Task<HttpResponseMessage> MoveAsync(
            string uri,
            string destination,
            bool overwrite,
            CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(new HttpMethod("MOVE"), uri);
            message.Headers.Add("Destination", destination);
            message.Headers.Add("Overwrite", overwrite ? "T" : "F");
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
