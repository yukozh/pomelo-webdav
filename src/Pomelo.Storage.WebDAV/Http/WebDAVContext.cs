// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.Storage.WebDAV.Lock;
using Pomelo.Storage.WebDAV.Storage;
using Pomelo.Storage.WebDAV.Utils;

namespace Pomelo.Storage.WebDAV.Http
{
    public class WebDAVContext
    {
        private IWebDAVStorageProvider storageProvider;
        private IWebDAVLockManager lockManager;
        private IRelativeUrlHelper relativeUrlHelper;

        public WebDAVContext(
            HttpContext httpContext, long defaultRequestMaxSize = 1024 * 1024 * 30)
        {
            HttpContext = httpContext;
            DefaultRequestMaxSize = defaultRequestMaxSize;
        }

#if NET5_0_OR_GREATER
        private long DefaultRequestMaxSize { get; init; }
#else
        private long DefaultRequestMaxSize { get; set; }
#endif

        internal static Dictionary<int, string> StatusCodeMapping = new Dictionary<int, string>
        {
            [100] = "Continue",
            [101] = "Switching Protocols",
            [200] = "OK",
            [201] = "Created",
            [202] = "Accepted",
            [203] = "Non-Authoritative Information",
            [204] = "No Content",
            [205] = "Reset Content",
            [206] = "Partial Content",
            [207] = "Multi-Status",
            [300] = "Multiple Choices",
            [301] = "Moved Permanently",
            [302] = "Found",
            [303] = "See Other",
            [304] = "Not Modified",
            [305] = "Use Proxy",
            [307] = "Temporary Redirect",
            [400] = "Bad Request",
            [401] = "Unauthorized",
            [402] = "Payment Required",
            [403] = "Forbidden",
            [404] = "Not Found",
            [405] = "Method Not Allowed",
            [406] = "Not Acceptable",
            [407] = "Proxy Authentication Required",
            [408] = "Request Time-out",
            [409] = "Conflict",
            [410] = "Gone",
            [411] = "Length Required",
            [412] = "Precondition Failed",
            [413] = "Request Entity Too Large",
            [414] = "Request-URI Too Large",
            [415] = "Unsupported Media Type",
            [416] = "Requested range not satisfiable",
            [417] = "Expectation Failed",
            [422] = "Unprocessable Entity",
            [424] = "Failed Dependency",
            [500] = "Internal Server Error",
            [501] = "Not Implemented",
            [502] = "Bad Gateway",
            [503] = "Service Unavailable",
            [504] = "Gateway Time-out"
        };

#if NET5_0_OR_GREATER
        public HttpContext HttpContext { get; init; }
#else
        public HttpContext HttpContext { get; set; }
#endif

        public IWebDAVStorageProvider Storage 
        {
            get 
            {
                if (storageProvider == null)
                {
                    storageProvider = RequestServices.GetRequiredService<IWebDAVStorageProvider>();
                }

                return storageProvider;
            }
        }

        public IWebDAVLockManager LockManager
        {
            get 
            {
                if (lockManager == null)
                {
                    lockManager = RequestServices.GetRequiredService<IWebDAVLockManager>();
                }

                return lockManager;
            }
        }

        public IRelativeUrlHelper RelativeUrlHelper
        {
            get 
            {
                if (relativeUrlHelper == null)
                {
                    relativeUrlHelper = RequestServices.GetRequiredService<IRelativeUrlHelper>();
                }

                return relativeUrlHelper;
            }
        }

        public ClaimsPrincipal User => HttpContext.User;

        public virtual IServiceProvider RequestServices 
            => HttpContext.RequestServices;

        public virtual CancellationToken RequestAborted 
            => HttpContext.RequestAborted;

        public virtual Stream RequestStream 
            => HttpContext.Request.Body;

        public virtual int Depth 
            => HttpContext.Request.Headers.ContainsKey("Depth")
            ? Convert.ToInt32(HttpContext.Request.Headers["Depth"].First())
            : 0;

        public virtual bool Overwrite
            => HttpContext.Request.Headers.ContainsKey("Overwrite")
            ? HttpContext.Request.Headers["Overwrite"].First().ToUpper() == "T"
            : false;

        public virtual long Timeout
        {
            get 
            {
                var timeoutString = HttpContext.Request.Headers.ContainsKey("Timeout")
                    ? HttpContext.Request.Headers["Timeout"].First()
                    : null;

                if (timeoutString.Contains(','))
                {
                    timeoutString = timeoutString.Split(',')[0].Trim();
                }

                if (!timeoutString.StartsWith("Second-", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException("Timeout only supports second");
                }

                return Convert.ToInt64(timeoutString.Substring("Second-".Length));
            }
        }

        public string Protocol => HttpContext.Request.Protocol;

        public string Method => HttpContext.Request.Method.ToUpper();

        public virtual string DecodedRelativeUri 
            => (HttpContext.Request.RouteValues["path"] as string ?? "").Trim('/');

        public virtual string EncodedRelativeUri 
            => string.Join('/', DecodedRelativeUri.Split('/').Select(x => HttpUtility.UrlPathEncode(x)));

        public virtual string EncodedFullUri => EncodedBaseUri + "/" + EncodedRelativeUri;

        public virtual string GetReasonPhase(int statusCode)
            => StatusCodeMapping.ContainsKey(statusCode) ? StatusCodeMapping[statusCode] : "Unknown";

        public virtual string EncodedBaseUri
            => RelativeUrlHelper.GetEncodedBaseUrl(this);

        public virtual bool IsResponded { get; protected set; }

        public virtual bool IsRequestBodyConsumed { get; set; }

        public virtual string EncodedFullDestination
        {
            get
            {
                return HttpContext.Request.Headers["Destination"].First();
            }
        }

        public virtual string DecodedFullDestination
        {
            get
            {
                return DecodeUri(EncodedFullDestination);
            }
        }

        public virtual string EncodedRelativeDestination
        {
            get
            {
                return ConvertFullEncodedUriToRelativeEncodedUri(HttpContext.Request.Headers["Destination"].First().Trim('/'));
            }
        }

        public virtual string DecodedRelativeDestination
        {
            get
            {
                return ConvertFullEncodedUriToRelativeDecodedUri(HttpContext.Request.Headers["Destination"].First().Trim('/'));
            }
        }

        public virtual string RequestBodyString { get; protected set; }

        public virtual async Task RespondWithoutBodyAsync(
            int statusCode,
            Dictionary<string, string> headers = null)
        { 
            if (IsResponded)
            {
                throw new InvalidOperationException("The request is already responded.");
            }

            ConsumeHeaders(headers);
            HttpContext.Response.StatusCode = statusCode;
            try
            {
                await HttpContext.Response.CompleteAsync();
            }
            catch { }
            IsResponded = true;
        }

        public virtual async Task RespondOkAsync(Dictionary<string, string> headers = null)
        {
            await RespondWithoutBodyAsync(200, headers);
        }

        public virtual async Task RespondCreatedAsync(Dictionary<string, string> headers = null)
        {
            await RespondWithoutBodyAsync(201, headers);
        }

        public virtual async Task RespondNotFoundAsync(Dictionary<string, string> headers = null)
        {
            await RespondWithoutBodyAsync(404, headers);
        }

        public virtual async Task RespondXmlAsync(
            int statusCode,
            string xml,
            Dictionary<string, string> headers = null)
        {
            if (IsResponded)
            {
                throw new InvalidOperationException("The request is already responded.");
            }

            ConsumeHeaders(headers);
            HttpContext.Response.StatusCode = statusCode;
            HttpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(xml);
            HttpContext.Response.ContentType = "text/xml; charset=utf-8";
            await HttpContext.Response.WriteAsync(xml);
            await HttpContext.Response.CompleteAsync();
            IsResponded = true;
        }

        public virtual async Task<bool> EnsureRequestSizeAsync()
        {
            if (HttpContext.Request.ContentLength.HasValue
                && HttpContext.Request.ContentLength.Value > DefaultRequestMaxSize)
            {
                HttpContext.Request.Body.Close();
                await RespondWithoutBodyAsync(413);
                return false;
            }

            return true;
        }

        public virtual async Task<string> ReadRequestBodyAsStringAsync()
        {
            if (!await EnsureRequestSizeAsync())
            {
                return null;
            }

            if (!IsRequestBodyConsumed)
            {
                using var streamReader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8);
                RequestBodyString = await streamReader.ReadToEndAsync();
                IsRequestBodyConsumed = true;
            }

            return RequestBodyString;
        }

        public virtual async Task<XDocument> ReadRequestBodyAsXDocumentAsync() 
        {
            if (!IsRequestBodyConsumed)
            {
                await ReadRequestBodyAsStringAsync();
            }

            return XDocument.Parse(RequestBodyString);
        }

        protected virtual void ConsumeHeaders(Dictionary<string, string> headers)
        {
            if (IsResponded)
            {
                throw new InvalidOperationException("The request is already responded.");
            }

            if (headers == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                HttpContext.Response.Headers[header.Key] = header.Value;
            }
        }

        public static string DecodeUri(string encodedUri) 
            => HttpUtility.UrlDecode(encodedUri);

        public static string EncodeUri(string decodedUri)
            => HttpUtility.UrlPathEncode(decodedUri);

        public string ConvertFullEncodedUriToRelativeEncodedUri(string fullUri)
            => RelativeUrlHelper.RemoveBaseUrl(this, fullUri);

        public string ConvertFullEncodedUriToRelativeDecodedUri(string fullUri)
            => DecodeUri(ConvertFullEncodedUriToRelativeEncodedUri(fullUri));

        public static string GetContainerFolder(string path)
        {
            var index = path.LastIndexOf('/');
            if (index < 1)
            {
                return "";
            }

            return path.Substring(0, index).Trim('/');
        }
}
}
