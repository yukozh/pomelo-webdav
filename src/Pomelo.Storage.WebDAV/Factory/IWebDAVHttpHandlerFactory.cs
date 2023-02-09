// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Pomelo.Storage.WebDAV.Http;

namespace Pomelo.Storage.WebDAV.Factory
{
    public interface IWebDAVHttpHandlerFactory
    {
        IWebDAVHttpHandler CreateHandler(HttpContext context);
    }
}
