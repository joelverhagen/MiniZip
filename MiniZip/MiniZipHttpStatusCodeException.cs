namespace Knapcode.MiniZip
{
    /// <summary>
    /// An exception related to an unexpected HTTP status code.
    /// </summary>
    public class MiniZipHttpStatusCodeException : MiniZipException
    {
        /// <summary>
        /// Initialize the exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MiniZipHttpStatusCodeException(string message) : base(message)
        {
        }
    }
}
