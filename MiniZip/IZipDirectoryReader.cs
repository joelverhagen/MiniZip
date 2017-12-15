using System;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An interface for reading ZIP directories.
    /// </summary>
    public interface IZipDirectoryReader : IDisposable
    {
        /// <summary>
        /// Reads the ZIP directory.
        /// </summary>
        /// <returns>The ZIP directory.</returns>
        Task<ZipDirectory> ReadAsync();
    }
}