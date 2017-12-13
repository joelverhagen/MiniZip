using System;

namespace Knapcode.MiniZip
{
    public class ZipBufferSizeProvider : IBufferSizeProvider
    {
        private readonly int _firstBufferSize;
        private int _nextValue;
        private readonly int _exponent;
        private bool _overflow;
        private bool _isFirst;
        
        public ZipBufferSizeProvider(int secondBufferSize, int exponent)
            : this(ZipConstants.EndOfCentralRecordBaseSize, secondBufferSize, exponent)
        {
        }

        public ZipBufferSizeProvider(int firstBufferSize, int secondBufferSize, int exponent)
        {
            _firstBufferSize = firstBufferSize;
            _nextValue = secondBufferSize;
            _exponent = exponent;
            _overflow = false;
            _isFirst = true;
        }

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
