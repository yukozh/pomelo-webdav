// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.Storage.WebDAV.Models;

namespace Pomelo.Storage.WebDAV.Storage
{
    public class LocalDiskWebDAVStorageProvider : IWebDAVStorageProvider
    {
        private static SHA256 SHA256 = SHA256.Create();
        private string localPath;

        public LocalDiskWebDAVStorageProvider(string localPath) 
        {
            this.localPath = localPath;
            if (!Directory.Exists(this.localPath))
            {
                Directory.CreateDirectory(this.localPath);
            }
        }

        public string LocalPath => localPath;

        public Task<IEnumerable<Item>> GetItemsAsync(string path, CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);

            var ret = new List<Item>();

            if (!Directory.Exists(physicalPath))
            {
                return Task.FromResult(ret as IEnumerable<Item>);
            }

            var di = new DirectoryInfo(physicalPath);
            ret.Add(new Item 
            {
                Href = string.Join("/", path.Replace("\\", "/").Split('/').Select(x => System.Web.HttpUtility.UrlPathEncode(x))),
                Properties = new ItemProperties 
                {
                    LastModified = di.LastWriteTime,
                    CreationTime = di.CreationTime,
                    ResourceType = ItemType.Directory,
                },
                Depth = 0
            });

            foreach (var directory in di.GetDirectories())
            {
                ret.Add(new Item
                {
                    Href = string.Join("/", Path.Combine(path, directory.Name).Replace("\\", "/").Split('/').Select(x => System.Web.HttpUtility.UrlPathEncode(x))),
                    Properties = new ItemProperties
                    {
                        LastModified = directory.LastWriteTime,
                        CreationTime = di.CreationTime,
                        ResourceType = ItemType.Directory,
                    },
                    Depth = 1
                });
            }

            foreach(var file in di.GetFiles())
            {
                var filePath = Path.Combine(path, file.Name);
                ret.Add(new Item
                {
                    Href = string.Join("/", filePath.Replace("\\", "/").Split('/').Select(x => System.Web.HttpUtility.UrlPathEncode(x))),
                    Properties = new ItemProperties
                    {
                        ContentLength = file.Length,
                        Etag = Convert.ToBase64String(SHA256.ComputeHash(Encoding.UTF8.GetBytes(filePath + file.LastWriteTime.Ticks))),
                        LastModified = file.LastWriteTime,
                        CreationTime = file.CreationTime,
                        ResourceType = ItemType.File
                    },
                    Depth = 1
                });
            }

            return Task.FromResult(ret as IEnumerable<Item>);
        }

        public Task<Stream> GetFileReadStreamAsync(
            string path, 
            CancellationToken cancellationToken)
        {
            var physicalPath = Path.Combine(this.localPath, path);
            if (!File.Exists(physicalPath))
            {
                return Task.FromResult<Stream>(null);
            }

            return Task.FromResult(new FileStream(physicalPath, FileMode.Open, FileAccess.Read) as Stream);
        }

        public Task<Stream> GetFileWriteStreamAsync(
            string path,
            long? requestedrequestedBytes = null,
            CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);
            return Task.FromResult(new FileStream(physicalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite) as Stream);
        }

        public Task<bool> IsFileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);
            return Task.FromResult(File.Exists(physicalPath));
        }

        public Task<bool> IsDirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);
            return Task.FromResult(Directory.Exists(physicalPath));
        }

        public Task<Item> GetItemAsync(string path, CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);
            if (Directory.Exists(physicalPath))
            {
                var di = new DirectoryInfo(physicalPath);
                return Task.FromResult(new Item 
                {
                    Depth = 0,
                    Href = string.Join("/", path.Replace("\\", "/").Split('/').Select(x => System.Web.HttpUtility.UrlPathEncode(x))),
                    Properties = new ItemProperties 
                    {
                        LastModified = di.LastWriteTime,
                        CreationTime = di.CreationTime,
                        ResourceType = ItemType.Directory
                    }
                });
            }
            else if (File.Exists(physicalPath))
            {
                var fi = new FileInfo(physicalPath);
                return Task.FromResult(new Item
                {
                    Depth = 0,
                    Href = string.Join("/", path.Replace("\\", "/").Split('/').Select(x => System.Web.HttpUtility.UrlPathEncode(x))),
                    Properties = new ItemProperties
                    {
                        ContentLength = fi.Length,
                        Etag = Convert.ToBase64String(SHA256.ComputeHash(Encoding.UTF8.GetBytes(path + fi.LastWriteTime.Ticks))),
                        LastModified = fi.LastWriteTime,
                        CreationTime = fi.CreationTime,
                        ResourceType = ItemType.File
                    }
                });
            }
            else
            {
                return Task.FromResult<Item>(null);
            }
        }

        public Task DeleteItemAsync(string path, CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);
            if (Directory.Exists(physicalPath))
            {
                Directory.Delete(physicalPath, true);
            }
            else if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            return Task.CompletedTask;
        }

        public Task<MoveOrCopyItemResult> MoveItemAsync(
            string fromPath,
            string destPath,
            bool overwrite,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                fromPath = "";
            }

            if (string.IsNullOrEmpty(destPath))
            {
                destPath = "";
            }

            if (fromPath == destPath)
            {
                return Task.FromResult(MoveOrCopyItemResult.Forbid);
            }

            fromPath = Path.Combine(this.localPath, fromPath);
            destPath = Path.Combine(this.localPath, destPath);

            if (File.Exists(fromPath))
            {
                try
                {
                    File.Move(fromPath, destPath, overwrite);
                    return Task.FromResult(MoveOrCopyItemResult.Ok);
                }
                catch (IOException)
                {
                    return Task.FromResult(MoveOrCopyItemResult.ResourceAlreadyExists);
                }
            }
            else if (Directory.Exists(fromPath))
            {
                if (Directory.Exists(destPath) && overwrite)
                {
                    Directory.Delete(destPath, true);
                }

                try
                {
                    Directory.Move(fromPath, destPath);
                    return Task.FromResult(MoveOrCopyItemResult.Ok);
                }
                catch (IOException)
                {
                    return Task.FromResult(MoveOrCopyItemResult.Conflict);
                }
            }

            return Task.FromResult(MoveOrCopyItemResult.Forbid);
        }

        public Task<MoveOrCopyItemResult> CopyItemAsync(
            string fromPath,
            string destPath,
            bool overwrite,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                fromPath = "";
            }

            if (string.IsNullOrEmpty(destPath))
            {
                destPath = "";
            }

            if (fromPath == destPath)
            {
                return Task.FromResult(MoveOrCopyItemResult.Forbid);
            }

            fromPath = Path.Combine(this.localPath, fromPath);
            destPath = Path.Combine(this.localPath, destPath);

            if (File.Exists(fromPath))
            {
                try
                {
                    File.Copy(fromPath, destPath, overwrite);
                    return Task.FromResult(MoveOrCopyItemResult.Ok);
                }
                catch (IOException)
                {
                    return Task.FromResult(MoveOrCopyItemResult.ResourceAlreadyExists);
                }
            }
            else if (Directory.Exists(fromPath))
            {
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);

                    try
                    {
                        CopyFolder(new DirectoryInfo(fromPath), new DirectoryInfo(destPath), overwrite);
                        return Task.FromResult(MoveOrCopyItemResult.Ok);
                    }
                    catch (IOException)
                    {
                        return Task.FromResult(MoveOrCopyItemResult.Conflict);
                    }
                }
            }

            return Task.FromResult(MoveOrCopyItemResult.Forbid);
        }

        private static void CopyFolder(DirectoryInfo source, DirectoryInfo target, bool overwrite)
        {
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), overwrite);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyFolder(diSourceSubDir, nextTargetSubDir, overwrite);
            }
        }

        public Task<IEnumerable<PatchPropertyResult>> PatchPropertyAsync(
            string path, 
            IEnumerable<XElement> elementsToSet,
            IEnumerable<XElement> elementsToRemove, 
            CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);

            // Ignore all properties
            var ret = new List<PatchPropertyResult>();

            foreach(var prop in elementsToSet)
            {
                var result = new PatchPropertyResult
                {
                    StatusCode = 424,
                    PropertyNames = new List<string>(),
                    Namespaces = prop.Descendants().Select(x => x.Name.NamespaceName).Distinct().ToList()
                };

                var items = prop.Descendants();
                var fi = new FileInfo(physicalPath);
                foreach (var item in items)
                { 
                    if (item.Name.LocalName.Equals("Win32FileAttributes", StringComparison.OrdinalIgnoreCase))
                    {
                        fi.Attributes = (FileAttributes)Convert.ToInt32(item.Value);
                        continue;
                    }

                    result.PropertyNames.Add("ns" + result.Namespaces.IndexOf(item.Name.NamespaceName) + ":" + item.Name.LocalName);
                }

                ret.Add(result);
            }

            foreach (var item in elementsToRemove)
            {
                var namespaces = item.Descendants().Select(x => x.Name.NamespaceName).Distinct().ToList();
                ret.Add(new PatchPropertyResult
                {
                    StatusCode = 424,
                    Namespaces = namespaces,
                    PropertyNames = item.Descendants().Select(x => "ns" + namespaces.IndexOf(x.Name.NamespaceName) + ":" + x.Name.LocalName).ToList()
                });
            }

            return Task.FromResult(ret as IEnumerable<PatchPropertyResult>);
        }

        public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            var physicalPath = Path.Combine(this.localPath, path);

            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
            }

            return Task.CompletedTask;
        }
    }

    public static class LocalDiskWebDAVStorageProviderExtensions
    {
        public static IServiceCollection AddLocalDiskWebDAVStorageProvider(this IServiceCollection services, string path)
            => services.AddSingleton<IWebDAVStorageProvider, LocalDiskWebDAVStorageProvider>(x => new LocalDiskWebDAVStorageProvider(path));
    }
}
