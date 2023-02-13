// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Pomelo.Storage.WebDAV.Lock;
using Pomelo.Storage.WebDAV.Models;
using Pomelo.Storage.WebDAV.Utils;

namespace Pomelo.Storage.WebDAV.Http
{
    public class DefaultWebDAVHttpHandler : WebDAVHttpHandlerBase
    {
        public DefaultWebDAVHttpHandler(
            HttpContext httpContext, 
            long defaultRequestMaxSize = 31457280) 
            : base(httpContext, defaultRequestMaxSize)
        {
        }

        public override async Task CopyAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            if (!await Storage.IsFileExistsAsync(DecodedRelativeUri, RequestAborted)
                && !await Storage.IsDirectoryExistsAsync(DecodedRelativeUri, RequestAborted))
            {
                await RespondNotFoundAsync();
                return;
            }

            await Storage.CopyItemAsync(
                DecodedRelativeUri,
                DecodedRelativeDestination,
                HttpContext.Request.Headers.ContainsKey("Overwrite")
                    ? HttpContext.Request.Headers["Overwrite"].ToString().ToUpper() == "T"
                    : true, HttpContext.RequestAborted);

            await RespondWihtoutBodyAsync(201, new Dictionary<string, string> 
            {
                ["Location"] = EncodeUri(EncodedFullDestination)
            });
        }

        public override async Task DeleteAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            await Storage.DeleteItemAsync(DecodedRelativeUri, RequestAborted);
            await LockManager.DeleteLockByUriAsync(EncodedRelativeUri, RequestAborted);
            await RespondOkAsync();
        }

        public override async Task GetAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            if (!await Storage.IsFileExistsAsync(DecodedRelativeUri, RequestAborted))
            {
                await RespondNotFoundAsync();
                return;
            }

            HttpContext.Response.Headers.Add("Accept-Ranges", "bytes");

            using var fs = await Storage.GetFileReadStreamAsync(DecodedRelativeUri, RequestAborted);
            if (HttpContext.Request.Headers.ContainsKey("Range") 
                && HttpContext.Request.Headers["Range"].ToString().StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            {
                var range = HttpContext.Request.Headers["Range"].ToString().Substring("bytes=".Length);
                var splited = range.Split('-');
                var from = Convert.ToInt64(string.IsNullOrEmpty(splited[0]) ? "0" : splited[0]);
                var to = Convert.ToInt64(string.IsNullOrEmpty(splited[1]) ? fs.Length.ToString() : splited[1]);
                var length = to - from + 1;
                HttpContext.Response.ContentLength = length;
                fs.Position = from;
                HttpContext.Response.Headers.Add("Content-Range", $"bytes {from}-{to}/{fs.Length}");
                HttpContext.Response.StatusCode = 206;
            }
            else
            {
                HttpContext.Response.StatusCode = 200;
            }
            HttpContext.Response.ContentType = "application/octet-stream";
            if (!HttpContext.Response.ContentLength.HasValue)
            {
                HttpContext.Response.ContentLength = fs.Length;
            }
            await fs.CopyToAsync(
                HttpContext.Response.Body,
                HttpContext.Response.ContentLength.Value, 
                81920, 
                RequestAborted);
            await HttpContext.Response.CompleteAsync();
        }

        public override async Task HeadAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            var info = await Storage.GetItemAsync(DecodedRelativeUri, RequestAborted);
            if (info == null)
            {
                await RespondNotFoundAsync();
                return;
            }

            HttpContext.Response.Headers.Add("Accept-Ranges", "bytes");

            await RespondOkAsync(new Dictionary<string, string> 
            {
                ["Etag"] = info.Properties.Etag,
                ["Last-Modified"] = (info.Properties.LastModified ?? DateTime.UtcNow).ToString("r"),
                ["Content-Length"] = info.Properties.ContentLength.ToString(),
                ["Content-Type"] = "application/octet-stream"
            });
        }

        public override async Task LockAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            Models.Lock _lock;

            if (HttpContext.Request.Headers.ContainsKey("If")) // Refresh Lock
            {
                var match = UrnRegex.Match(HttpContext.Request.Headers["If"]);
                if (match == null)
                {
                    await RespondWihtoutBodyAsync(400);
                    return;
                }

                var lockToken = Guid.Parse(match.Value);
                _lock = await LockManager.RefreshLock(lockToken, Timeout);
            }
            else
            {
                XDocument doc = await ReadRequestBodyAsXDocumentAsync();
                var element = doc.Descendants("{DAV:}owner").Descendants("{DAV:}href").First();
                var owner = element.Value;

                var exclusive = doc.Descendants("{DAV:}exclusive");
                var lockType = exclusive == null
                    ? LockType.Shared
                    : LockType.Exclusive;

                try
                {
                    _lock = await LockManager.LockAsync(
                        EncodedRelativeUri,
                        Depth,
                        lockType,
                        owner,
                        Timeout,
                        RequestAborted);
                }
                catch (LockException)
                {
                    await RespondWihtoutBodyAsync(409);
                    return;
                }
            }

            var response = $"""
                    <?xml version="1.0" encoding="utf-8"?> 
                    <D:prop xmlns:D="DAV:"> 
                        <D:lockdiscovery> 
                            <D:activelock> 
                                <D:locktype><D:write/></D:locktype> 
                                <D:lockscope><D:{_lock.Type.ToString().ToLower()}/></D:lockscope> 
                                <D:depth>{(_lock.Depth == -1 ? "infinity" : _lock.Depth)}</D:depth> 
                                <D:owner> 
                                    <D:href>{_lock.Owner}</D:href> 
                                </D:owner> 
                                <D:timeout>Second-{_lock.RequestedTimeoutSeconds}</D:timeout> 
                                <D:locktoken> 
                                    <D:href>{LockManager.Schema}:{_lock.LockToken}</D:href>
                                </D:locktoken>
                                <D:lockroot> 
                                    <D:href>{EncodedBaseUri}/{_lock.EncodedRelativeUri}</D:href>
                                </D:lockroot> 
                            </D:activelock> 
                        </D:lockdiscovery> 
                    </D:prop>
                    """;
            await RespondXmlAsync(200, response);
        }

        public override async Task MkcolAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            if (await Storage.IsDirectoryExistsAsync(DecodedRelativeUri, RequestAborted))
            {
                await RespondWihtoutBodyAsync(409);
                return;
            }

            await Storage.CreateDirectoryAsync(DecodedRelativeUri, RequestAborted);
            await RespondCreatedAsync();
        }

        public override async Task MoveAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            if (!await Storage.IsFileExistsAsync(DecodedRelativeUri, RequestAborted)
                && !await Storage.IsDirectoryExistsAsync(DecodedRelativeUri, RequestAborted))
            {
                await RespondNotFoundAsync();
                return;
            }

            await Storage.MoveItemAsync(
                DecodedRelativeUri,
                DecodedRelativeDestination,
                Overwrite,
                RequestAborted);

            await LockManager.DeleteLockByUriAsync(EncodedRelativeUri, RequestAborted);

            await RespondCreatedAsync(new Dictionary<string, string> 
            {
                ["Location"] = EncodedFullDestination
            });
        }

        public override async Task PropFindAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            var items = await Storage.GetItemsAsync(
                DecodedRelativeUri,
                RequestAborted);

            var isDirectory = string.IsNullOrEmpty(DecodedRelativeUri) || await Storage.IsDirectoryExistsAsync(DecodedRelativeUri);
            if (isDirectory && (items == null || items.Count() == 0))
            {
                await RespondNotFoundAsync();
                return;
            }

            if (Depth > 1)
            {
                await RespondWihtoutBodyAsync(400);
                return;
            }

            string response = null;
            if (isDirectory)
            {
                response = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <D:multistatus xmlns:D="DAV:">
                {await PropFindResponseBuildHelper.BuildSingleDirectoryAsync(items.First(x => x.Depth == 0), EncodedBaseUri, Protocol, LockManager, RequestAborted)}
                {(Depth == 0 ? "" : await PropFindResponseBuildHelper.BuildDirectoriesAsync(items.Where(x => x.Properties.ResourceType == ItemType.Directory && x.Depth > 0), EncodedBaseUri, Protocol, LockManager, RequestAborted))}
                {(Depth == 0 ? "" : await PropFindResponseBuildHelper.BuildFilesAsync(items.Where(x => x.Properties.ResourceType == ItemType.File), EncodedBaseUri, Protocol, LockManager, RequestAborted))}
                </D:multistatus>
                """;
            }
            else
            {
                var item = await Storage.GetItemAsync(DecodedRelativeUri, RequestAborted);
                if (item == null)
                {
                    await RespondNotFoundAsync();
                    return;
                }

                response = await PropFindResponseBuildHelper.BuildSingleFileAsync(item, EncodedBaseUri, Protocol, LockManager, RequestAborted);
            }

            await RespondXmlAsync(207, response);
        }

        public override async Task PropPatchAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return;
            }

            if (!await Storage.IsFileExistsAsync(DecodedRelativeUri, RequestAborted))
            {
                await RespondNotFoundAsync();
                return;
            }

            var doc = await ReadRequestBodyAsXDocumentAsync();
            var set = doc.Descendants("{DAV:}set").FirstOrDefault();
            var remove = doc.Descendants("{DAV:}remove").FirstOrDefault();
            var elementsToSet = set != null
                ? set.Descendants().ToList()
                : PropPatchResponseBuildHelper.EmptyXElementList;
            var elementsToRemove = remove != null
                ? remove.Descendants().ToList()
                : PropPatchResponseBuildHelper.EmptyXElementList;
            var results = await Storage.PatchPropertyAsync(
                DecodedRelativeUri,
                elementsToSet,
                elementsToRemove,
                RequestAborted);

            var response = $"""
                <?xml version="1.0" encoding="utf-8" ?> 
                <D:multistatus xmlns:D="DAV:"> 
                    <D:response> 
                        <D:href>{EncodedBaseUri}/{EncodedRelativeUri}</D:href> 
                        {PropPatchResponseBuildHelper.BuildPropStat(results, Protocol)}
                    </D:response> 
                </D:multistatus> 
                """;

            await RespondXmlAsync(207, response);
        }

        public override async Task PutAsync()
        {
            if (!await Storage.IsDirectoryExistsAsync(GetContainerFolder(DecodedRelativeUri), RequestAborted))
            {
                await RespondWihtoutBodyAsync(409);
                return;
            }

            HttpContext.Response.Headers.Add("Accept-Ranges", "bytes");

            using var fs = await Storage.GetFileWriteStreamAsync(DecodedRelativeUri, RequestAborted);
            var statusCode = 204;
            if (HttpContext.Request.Headers.ContainsKey("Range")
                && HttpContext.Request.Headers["Range"].ToString().StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            {
                var range = HttpContext.Request.Headers["Range"].ToString().Substring("bytes=".Length);
                var splited = range.Split('-');
                var from = Convert.ToInt64(string.IsNullOrEmpty(splited[0]) ? "0" : splited[0]);
                var to = Convert.ToInt64(string.IsNullOrEmpty(splited[1]) ? fs.Length.ToString() : splited[1]);
                var length = to - from + 1;
                fs.Position = from;
                HttpContext.Response.Headers.Add("Content-Range", $"bytes {from}-{to}/*");
                statusCode = 206;
                await HttpContext.Request.Body.CopyToAsync(fs, length, 81920, RequestAborted);
            }
            else
            {
                await HttpContext.Request.Body.CopyToAsync(fs, RequestAborted);
            }

            await RespondWihtoutBodyAsync(statusCode);
        }

        public override async Task UnlockAsync()
        {
            var lockToken = HttpContext.Request.Headers["Lock-Token"].ToString().TrimStart('<').TrimEnd('>');
            var lockTokenGuid = lockToken.Substring(LockManager.Schema.Length + 1);
            await LockManager.UnlockAsync(Guid.Parse(lockTokenGuid));
            await RespondWihtoutBodyAsync(204);
        }
    }
}
