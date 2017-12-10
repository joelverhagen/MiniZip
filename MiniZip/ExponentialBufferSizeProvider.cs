using System;

namespace MiniZip
{
    public class ExponentialBufferSizeProvider : IBufferSizeProvider
    {
        private int _nextValue;
        private readonly int _exponent;
        private bool _overflow;

        public ExponentialBufferSizeProvider(int initialValue, int exponent)
        {
            _nextValue = initialValue;
            _exponent = exponent;
            _overflow = false;
        }

        public int GetNextBufferSize()
        {
            if (_overflow)
            {
                return int.MaxValue;
            }

            var output = _nextValue;

            try
            {
                _nextValue *= _exponent;
            }
            catch (OverflowException)
            {
                _overflow = true;
            }

            return output;
        }
    }
}
