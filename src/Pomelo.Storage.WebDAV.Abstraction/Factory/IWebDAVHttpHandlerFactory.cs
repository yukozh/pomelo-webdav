// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Pomelo.Storage.WebDAV.Abstractions.Http;

namespace Pomelo.Storage.WebDAV.Abstractions.Factory
{
    public interface IWebDAVHttpHandlerFactory
    {
        IWebDAVHttpHandler CreateHandler(HttpContext context);
    }
}
