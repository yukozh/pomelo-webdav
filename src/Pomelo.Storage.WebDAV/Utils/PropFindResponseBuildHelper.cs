// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Pomelo.Storage.WebDAV.Lock;
using Pomelo.Storage.WebDAV.Models;

namespace Pomelo.Storage.WebDAV.Utils
{
    public static class PropFindResponseBuildHelper
    {
        private static string ParseUrl(string url)
        {
            return url;
        }

        private static string ParseCreationTime(DateTime? time)
        {
            if (time == null)
            {
                time = DateTime.UtcNow;
            }

            return time.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
        }

        public static async Task<string> BuildSingleDirectoryAsync(
            Item item,
            string baseUrl,
            string protocol,
            IWebDAVLockManager lockManager,
            CancellationToken cancellationToken = default)
            => $"""
                <D:response>
                    <D:href>{baseUrl}/{HttpUtility.UrlPathEncode(item.Href)}</D:href>
                    <D:propstat>
                        <D:status>{protocol} 200 OK</D:status>
                        <D:prop>
                            <D:getlastmodified>{(item.Properties.LastModified ?? DateTime.UtcNow).ToString("r")}</D:getlastmodified>
                            <D:creationdate>{ParseCreationTime(item.Properties.CreationTime)}</D:creationdate>
                            <D:getcontentlength>{item.Properties.ContentLength}</D:getcontentlength>
                            <D:resourcetype>
                                <D:collection/>
                            </D:resourcetype>
                            { (item.DisplayName != null ? $"<D:displayname>{SecurityElement.Escape(item.DisplayName)}</D:displayname>" : "<D:displayname/>") }
                            {await BuildLocksAsync(HttpUtility.UrlPathEncode(item.Href), lockManager, cancellationToken)}
                        </D:prop>
                    </D:propstat>
                </D:response>
             """;

        public static async Task<string> BuildLocksAsync(string uri, IWebDAVLockManager lockManager, CancellationToken cancellationToken = default)
        {
            uri = uri.Trim('/');
            var locks = await lockManager.GetLocksAsync(uri, cancellationToken);
            if (!locks.Any())
            {
                return "";
            }

            return $"""
                               <D:supportedlock>
                                   <D:lockentry>
                                       <D:lockscope>
                                           <D:{locks.First().Type.ToString().ToLowerInvariant()}/>
                                       </D:lockscope>
                                       <D:locktype>
                                           <D:write/>
                                       </D:locktype>
                                   </D:lockentry>
                               </D:supportedlock>
                """;
        }

        public static async Task<string> BuildDirectoriesAsync(
            IEnumerable<Item> items,
            string baseUrl,
            string protocol,
            IWebDAVLockManager lockManager,
            CancellationToken cancellationToken = default)
        {
            var stringBuilder = new StringBuilder();

            foreach (var item in items)
            {
                stringBuilder.AppendLine(await BuildSingleDirectoryAsync(item, baseUrl, protocol, lockManager, cancellationToken));
            }

            return stringBuilder.ToString();
        }

        public static async Task<string> BuildSingleFileAsync(
            Item item,
            string baseUrl,
            string protocol,
            IWebDAVLockManager lockManager,
            CancellationToken cancellationToken = default)
            => $"""
                    <D:response>
                        <D:href>{baseUrl}/{HttpUtility.UrlPathEncode(item.Href)}</D:href>
                        <D:propstat>
                            <D:status>{protocol} 200 OK</D:status>
                            <D:prop>
                                <D:resourcetype/>
                                <D:getcontentlength>{item.Properties.ContentLength}</D:getcontentlength>
                                <D:getetag>{item.Properties.Etag}</D:getetag>
                                <D:getcontenttype/>
                                <D:getlastmodified>{(item.Properties.LastModified ?? DateTime.UtcNow).ToString("r")}</D:getlastmodified>
                                <D:creationdate>{ParseCreationTime(item.Properties.CreationTime)}</D:creationdate>
                                {(item.DisplayName != null ? $"<D:displayname>{SecurityElement.Escape(item.DisplayName)}</D:displayname>" : "<D:displayname/>")}
                                {await BuildLocksAsync(HttpUtility.UrlPathEncode(item.Href), lockManager, cancellationToken)}
                            </D:prop>
                        </D:propstat>
                    </D:response>
                 """;

        public static async Task<string> BuildFilesAsync(
            IEnumerable<Item> items,
            string baseUrl,
            string protocol,
            IWebDAVLockManager lockManager,
            CancellationToken cancellationToken = default)
        {
            var stringBuilder = new StringBuilder();

            foreach (var item in items)
            {
                stringBuilder.AppendLine(await BuildSingleFileAsync(item, baseUrl, protocol, lockManager, cancellationToken));
            }

            return stringBuilder.ToString();
        }
    }
}
