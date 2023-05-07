// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Pomelo.Storage.WebDAV.Http;
using Pomelo.Storage.WebDAV.Models;

namespace Pomelo.Storage.WebDAV.Utils
{
    public static class PropPatchResponseBuildHelper
    {
        public static readonly IReadOnlyList<XElement> EmptyXElementList = new List<XElement>();

        public static string BuildPropStat(IEnumerable<PatchPropertyResult> results, string protocol)
        {
            var builder = new StringBuilder();
            foreach (var result in results)
            {
                if (result.PropertyNames.Count == 0)
                {
                    continue;
                }

                builder.AppendLine(BuildSinglePropStat(result, protocol));
            }
            return builder.ToString();
        }

        public static string BuildSinglePropStat(PatchPropertyResult result, string protocol)
        {
            return $"""
                        <D:propstat> 
                            <D:prop {BuildXmlns(result.Namespaces)}>
                                {BuildProps(result.PropertyNames)}
                            </D:prop> 
                            <D:status>{protocol} {result.StatusCode} {(WebDAVContext.StatusCodeMapping.ContainsKey(result.StatusCode) ? WebDAVContext.StatusCodeMapping[result.StatusCode] : "Unknown")}</D:status> 
                        </D:propstat>
                """;
        }

        public static string BuildProps(IEnumerable<string> names)
        {
            var builder = new StringBuilder();

            foreach (var name in names)
            {
                builder.AppendLine($"""
                                <{name}/>
                """);
            }

            return builder.ToString();
        }

        public static string BuildXmlns(IEnumerable<string> namespaces)
        {
            var builder = new StringBuilder();

            var index = 0;
            foreach (var ns in namespaces)
            {
                builder.Append($" xmlns:ns{index++}=\"{ns}\"");
            }

            return builder.ToString();
        }
    }
}
