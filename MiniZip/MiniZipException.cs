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

        /// <summary>
        /// Initialize the exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MiniZipException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
