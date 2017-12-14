using System;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public interface IHttpZipProvider : IDisposable
    {
        int BufferGrowthExponent { get; set; }
        int FirstBufferSize { get; set; }
        int SecondBufferSize { get; set; }

        Task<ZipDirectoryReader> GetReaderAsync(Uri requestUri);
    }
}