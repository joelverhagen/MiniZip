using System;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An specific exception for this library.
    /// </summary>
    public class MiniZipException : Exception
    {
        /// <summary>
        /// Initialize the exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MiniZipException(string message)
            : base(message)
        {
        }
    }
}
