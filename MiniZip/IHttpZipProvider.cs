using System;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An interface for getting a <see cref="ZipDirectoryReader"/> from a URL.
    /// </summary>
    public interface IHttpZipProvider
    {
        /// <summary>
        /// The exponent to determine the buffer growth rate.
        /// </summary>
        int BufferGrowthExponent { get; set; }

        /// <summary>
        /// The first buffer size in bytes to use when reading.
        /// </summary>
        int FirstBufferSize { get; set; }

        /// <summary>
        /// The second buffer size in bytes to use when reading. This defaults to 4096 bytes.
        /// </summary>
        int SecondBufferSize { get; set; }

        /// <summary>
        /// Initialize the ZIP directory reader for the provided request URL.
        /// </summary>
        /// <param name="requestUri">The request URL.</param>
        /// <returns>The ZIP directory reader.</returns>
        Task<ZipDirectoryReader> GetReaderAsync(Uri requestUri);
    }
}