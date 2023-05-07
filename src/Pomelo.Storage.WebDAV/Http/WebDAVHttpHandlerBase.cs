// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Pomelo.Storage.WebDAV.Lock;

namespace Pomelo.Storage.WebDAV.Http
{
    public abstract class WebDAVHttpHandlerBase : WebDAVContext, IWebDAVHttpHandler
    {
        public WebDAVHttpHandlerBase(
            HttpContext httpContext,
            long defaultRequestMaxSize = 31457280)
            : base(httpContext, defaultRequestMaxSize)
        { }

        public abstract Task CopyAsync();

        public abstract Task DeleteAsync();

        public abstract Task GetAsync();

        public abstract Task HeadAsync();

        public abstract Task LockAsync();

        public abstract Task MkcolAsync();

        public abstract Task MoveAsync();

        public virtual async Task OptionsAsync()
        {
            await RespondWithoutBodyAsync(200, new Dictionary<string, string>
            {
                ["Allow"] = string.Join(", ", AllowedMethods),
                ["Public"] = string.Join(", ", AllowedMethods),
                ["Dav"] = "1, 2, 3"
            });
        }

        public abstract Task PropFindAsync();

        public abstract Task PropPatchAsync();

        public abstract Task PutAsync();

        public abstract Task UnlockAsync();

        private readonly static string[] AllowedMethods = new[]
        {
            "OPTIONS",
            "GET",
            "HEAD",
            "PUT",
            "POST",
            "COPY",
            "PROPFIND",
            "PROPPATCH",
            "MKCOL",
            "DELETE",
            "MOVE",
            "LOCK",
            "UNLOCK"
        };

        public virtual async Task<bool> IsAbleToMoveOrCopyAsync()
        {
            if (!await IsAbleToWriteAsync())
            {
                return false;
            }

            return await IsAbleToWriteAsync(EncodedRelativeDestination);
        }

        public virtual async Task<bool> IsAbleToWriteAsync(string uri = null)
        {
            if (uri == null)
            {
                uri = EncodedRelativeUri;
            }

            var locks = (await LockManager.GetLocksAsync(uri, RequestAborted));
            var lockTokens = GetRequestLockTokens();
            if (locks.Any(x => !lockTokens.Contains(x.LockToken) && x.Type == LockType.Exclusive))
            {
                await RespondWithoutBodyAsync(423);
                return false;
            }
            else
            {
                return true;
            }
        }

        public virtual async Task<bool> IsAbleToReadAsync()
        {
            var locks = (await LockManager.GetLocksAsync(EncodedRelativeUri, RequestAborted));
            var lockTokens = GetRequestLockTokens();
            if (locks.Any(x => !lockTokens.Contains(x.LockToken) && x.Type == LockType.Exclusive))
            {
                await RespondWithoutBodyAsync(423);
                return false;
            }
            else
            {
                return true;
            }
        }

        private Regex urnRegex;

        protected virtual Regex UrnRegex
        {
            get 
            {
                if (urnRegex == null)
                {
                    urnRegex = new Regex("(?<=" + LockManager.Schema + ":)[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}");
                }

                return urnRegex;
            }
        }

        protected virtual IEnumerable<Guid> GetRequestLockTokens()
        {
            var _if = HttpContext.Request.Headers["If"].ToString();
            if (string.IsNullOrEmpty(_if))
            {
                return new Guid[] { };
            }

            return UrnRegex.Matches(_if).Cast<Match>().Select(x => Guid.Parse(x.Value));
        }
    }
}
