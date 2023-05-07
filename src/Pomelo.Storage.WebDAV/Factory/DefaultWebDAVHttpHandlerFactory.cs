// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.Storage.WebDAV.Http;
using Pomelo.Storage.WebDAV.Utils;

namespace Pomelo.Storage.WebDAV.Factory
{
    public class DefaultWebDAVHttpHandlerFactory : IWebDAVHttpHandlerFactory
    {
        private long defaultRequestMaxSize;

        public DefaultWebDAVHttpHandlerFactory(long defaultRequestMaxSize = 31457280)
        {
            this.defaultRequestMaxSize = defaultRequestMaxSize;
        }

        public IWebDAVHttpHandler CreateHandler(HttpContext context)
        {
            return new DefaultWebDAVHttpHandler(context, defaultRequestMaxSize);
        }
    }

    public static class DefaultWebDAVHttpHandlerFactoryExtensions
    {
        public static IServiceCollection AddDefaultWebDAVHttpHandlerFactory(
            this IServiceCollection services, 
            long defaultRequestMaxSize = 31457280)
            => services.AddSingleton<IWebDAVHttpHandlerFactory>(x => new DefaultWebDAVHttpHandlerFactory(defaultRequestMaxSize)).AddDefaultRelativeUrlHelper();
    }
}
