using System;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

namespace Pomelo.Storage.WebDAV.ContentType
{
    public static class ContentTypeProviderExtensions
    {
        public static IServiceCollection AddFileExtensionContentTypeProvider(
            this IServiceCollection services,
            Action<FileExtensionContentTypeProvider> configure = null)
            => services.AddSingleton<IContentTypeProvider>(x => 
            {
                var provider = new FileExtensionContentTypeProvider();
                configure?.Invoke(provider);
                return provider;
            });
    }
}
