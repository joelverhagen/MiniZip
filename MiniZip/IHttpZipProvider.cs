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
        /// Initialize the ZIP directory reader for the provided request URL.
        /// </summary>
        /// <param name="requestUri">The request URL.</param>
        /// <returns>The ZIP directory reader.</returns>
        Task<ZipDirectoryReader> GetReaderAsync(Uri requestUri);
    }
}