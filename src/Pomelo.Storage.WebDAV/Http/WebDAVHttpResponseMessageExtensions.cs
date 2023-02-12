using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Pomelo.Storage.WebDAV.Models;

namespace Pomelo.Storage.WebDAV.Http
{
    public static class WebDAVHttpResponseMessageExtensions
    {
        private static readonly string[] AcceptedContentTypes = new[] { "text/xml", "application/xml" };

        #region PROPFIND
        public static async Task<IEnumerable<PropFindResult>> ToPropFindResultsAsync(
            this HttpResponseMessage response,
            CancellationToken cancellationToken = default)
        {
            if (!AcceptedContentTypes.Contains(response.Content.Headers.ContentType.MediaType.ToLower()))
            {
                throw new InvalidDataException("Response body is not XML");
            }

            if (response.StatusCode != System.Net.HttpStatusCode.MultiStatus)
            {
                throw new InvalidDataException("The response is not multi status");
            }

            var result = new List<PropFindResult>();
            var doc = XDocument.Parse(await response.Content.ReadAsStringAsync());
            var responses = doc.Root.Descendants("{DAV:}response");
            foreach(var res in responses)
            {
                var href = res.Descendants("{DAV:}href").FirstOrDefault();
                if (href == null)
                {
                    continue;
                }

                var propstat = res.Descendants("{DAV:}propstat").FirstOrDefault();
                if (propstat == null)
                {
                    continue;
                }

                var status = propstat.Descendants("{DAV:}status").FirstOrDefault();
                if (status == null)
                {
                    continue;
                }

                var prop = propstat.Descendants("{DAV:}prop").FirstOrDefault();
                if (prop == null)
                {
                    continue;
                }
                var properties = prop.Descendants();

                result.Add(new PropFindResult 
                {
                    Href = href.Value,
                    PropStat = new PropFindResultPropStat 
                    {
                        Properties = properties,
                        Status = new ResponseStatus(status.Value)
                    }
                });
            }
            return result;
        }
        #endregion

        #region OPTIONS
        public static IEnumerable<string> ToOptionsResult(
            this HttpResponseMessage response) 
        {
            return response.Content.Headers.GetValues("Allow");
        }
        #endregion

        #region HEAD
        public static HeadResult ToHeadResult(
            this HttpResponseMessage response)
        {
            return new HeadResult
            {
                ContentLength = response.Content.Headers.ContentLength ?? 0,
                AcceptRanges = response.Headers.Contains("Accept-Ranges") 
                    ? string.Join(",", response.Headers.GetValues("Accept-Ranges"))
                    : null,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                Etag = response.Headers.Contains("Etag") 
                    ? string.Join(",", response.Headers.GetValues("Etag"))
                    : null,
                LastModified = response.Content.Headers.Contains("Last-Modified")
                    ? Convert.ToDateTime(response.Content.Headers.GetValues("Last-Modified").First())
                    : null
            };
        }
        #endregion
    }
}
