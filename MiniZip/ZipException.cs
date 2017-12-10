using System;

namespace Knapcode.MiniZip
{
    public class ZipException : Exception
    {
        public ZipException(string message)
            : base(message)
        {
        }
    }
}
