namespace Knapcode.MiniZip
{
    /// <summary>
    /// A buffer size provider that always returns 0.
    /// </summary>
    public class NullBufferSizeProvider : IBufferSizeProvider
    {
        /// <summary>
        /// A shared instance of the null buffer size provider.
        /// </summary>
        public static NullBufferSizeProvider Instance { get; } = new NullBufferSizeProvider();

        /// <summary>
        /// Returns an null buffer size of 0.
        /// </summary>
        /// <returns>The next buffer size, 0.</returns>
        public int GetNextBufferSize() => 0;
    }
}
