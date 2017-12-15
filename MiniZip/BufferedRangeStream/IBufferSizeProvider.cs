namespace Knapcode.MiniZip
{
    /// <summary>
    /// An interface for providing different buffer sizes over time. This is used by <see cref="BufferedRangeStream"/>
    /// to implement buffer growth algorithms (such as exponential growth).
    /// </summary>
    public interface IBufferSizeProvider
    {
        /// <summary>
        /// Determines the next buffer size to used. 
        /// </summary>
        /// <returns>The next buffer size.</returns>
        int GetNextBufferSize();
    }
}
