using System;

namespace Knapcode.MiniZip
{
    public class MiniZipException : Exception
    {
        public MiniZipException(string message)
            : base(message)
        {
        }
    }
}
