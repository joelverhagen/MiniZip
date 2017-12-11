using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public interface IRangeReader
    {
        Task<int> ReadAsync(long srcOffset, byte[] dst, int dstOffset, int count);
    }
}
