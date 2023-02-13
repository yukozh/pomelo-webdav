using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pomelo.Storage.WebDAV.Utils
{
    public static class StreamExtensions
    {
        public static async ValueTask CopyToAsync(
            this Stream stream, 
            Stream destination,
            long bytes, 
            int bufferSize = 81920,
            CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var read = 0L;
            while(read < bytes)
            {
                var r = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, bytes - read), cancellationToken);
                read += r;
                await destination.WriteAsync(buffer, 0, r);
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
