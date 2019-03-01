using System.Net;

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
        /// <param name="statusCode">The HTTP status code that caused this exception.</param>
        /// <param name="reasonPhrase">The reason phrase returned with the <paramref name="statusCode"/>.</param>
        public MiniZipHttpStatusCodeException(string message, HttpStatusCode statusCode, string reasonPhrase) : base(message)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
        }

        /// <summary>
        /// The HTTP status code that caused this exception.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The HTTP reason phrase returned with the <see cref="StatusCode"/>.
        /// </summary>
        public string ReasonPhrase { get; }
    }
}
