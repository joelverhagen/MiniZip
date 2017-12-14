using System;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public interface IZipDirectoryReader : IDisposable
    {
        Task<ZipDirectory> ReadAsync();
    }
}