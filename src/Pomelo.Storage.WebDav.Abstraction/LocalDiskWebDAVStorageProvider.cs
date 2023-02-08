using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pomelo.Storage.WebDav.Abstractions
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

        public Task<IEnumerable<Item>> GetItemsAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }

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
                        SupportedLock = new ItemLock
                        {
                            LockScopes = new[] { "exclusive" },
                            LockTypes = new[] { "write" }
                        },
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
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }

            var physicalPath = Path.Combine(this.localPath, path);
            if (!File.Exists(physicalPath))
            {
                return Task.FromResult<Stream>(null);
            }

            return Task.FromResult(new FileStream(physicalPath, FileMode.Open, FileAccess.Read) as Stream);
        }

        public Task<Stream> GetFileWriteStreamAsync(
            string path,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }

            var physicalPath = Path.Combine(this.localPath, path);
            return Task.FromResult(new FileStream(physicalPath, FileMode.CreateNew, FileAccess.ReadWrite) as Stream);
        }

        public Task<bool> IsFileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }

            var physicalPath = Path.Combine(this.localPath, path);
            return Task.FromResult(File.Exists(physicalPath));
        }

        public Task<bool> IsDirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }

            var physicalPath = Path.Combine(this.localPath, path);
            return Task.FromResult(Directory.Exists(physicalPath));
        }

        public Task<Item> GetItemAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }

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
                        SupportedLock = new ItemLock
                        {
                            LockScopes = new[] { "exclusive" },
                            LockTypes = new[] { "write" }
                        },
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
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }

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

        public Task MoveItemAsync(
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
                return Task.CompletedTask;
            }

            fromPath = Path.Combine(this.localPath, fromPath);
            destPath = Path.Combine(this.localPath, destPath);

            if (File.Exists(fromPath))
            {
                File.Move(fromPath, destPath, overwrite);
            }
            else if (Directory.Exists(fromPath))
            {
                if (Directory.Exists(destPath) && overwrite)
                {
                    Directory.Delete(destPath, true);
                }

                Directory.Move(fromPath, destPath);
            }

            return Task.CompletedTask;
        }

        public Task CopyItemAsync(
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
                return Task.CompletedTask;
            }

            fromPath = Path.Combine(this.localPath, fromPath);
            destPath = Path.Combine(this.localPath, destPath);

            if (File.Exists(fromPath))
            {
                File.Copy(fromPath, destPath, true);
            }
            else if (Directory.Exists(fromPath))
            {
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);

                    CopyFolder(new DirectoryInfo(fromPath), new DirectoryInfo(destPath));
                }
            }

            return Task.CompletedTask;
        }

        private static void CopyFolder(DirectoryInfo source, DirectoryInfo target)
        {
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyFolder(diSourceSubDir, nextTargetSubDir);
            }
        }
    }

    public static class LocalDiskWebDAVStorageProviderExtensions
    {
        public static IServiceCollection AddLocalDiskWebDAVStorageProvider(this IServiceCollection services, string path)
            => services.AddSingleton<IWebDAVStorageProvider, LocalDiskWebDAVStorageProvider>(x => new LocalDiskWebDAVStorageProvider(path));
    }
}
