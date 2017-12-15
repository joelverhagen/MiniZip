using System;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// A buffer size provider designed for reading ZIP files with <see cref="BufferedRangeStream"/>.
    /// </summary>
    public class ZipBufferSizeProvider : IBufferSizeProvider
    {
        private readonly int _firstBufferSize;
        private int _nextValue;
        private readonly int _exponent;
        private bool _overflow;
        private bool _isFirst;

        /// <summary>
        /// Initializes an instance of the ZIP buffer provider.
        /// </summary>
        /// <param name="firstBufferSize">The first buffer size to use.</param>
        /// <param name="secondBufferSize">The second buffer size to use.</param>
        /// <param name="exponent">The exponent used to grow the buffer size.</param>
        public ZipBufferSizeProvider(int firstBufferSize, int secondBufferSize, int exponent)
        {
            _firstBufferSize = firstBufferSize;
            _nextValue = secondBufferSize;
            _exponent = exponent;
            _overflow = false;
            _isFirst = true;
        }

        /// <summary>
        /// Gets the next buffer size. If the buffer size has grown over <see cref="int.MaxValue"/>, then
        /// <see cref="int.MaxValue"/> is returned.
        /// </summary>
        /// <returns>The buffer size.</returns>
        public int GetNextBufferSize()
        {
            if (_isFirst)
            {
                _isFirst = false;
                return _firstBufferSize;
            }

            if (_overflow)
            {
                return int.MaxValue;
            }

            var output = _nextValue;

            try
            {
                _nextValue = checked(_nextValue  * _exponent);
            }
            catch (OverflowException)
            {
                _overflow = true;
            }

            return output;
        }
    }
}
