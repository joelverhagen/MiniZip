using System;
using System.Net;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An exception related to an unexpected HTTP response.
    /// </summary>
    public class MiniZipHttpException : MiniZipException
    {
        /// <summary>
        /// Initialize the exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="statusCode">The HTTP status code that caused this exception.</param>
        /// <param name="reasonPhrase">The reason phrase returned with the <paramref name="statusCode"/>.</param>
        public MiniZipHttpException(string message, HttpStatusCode statusCode, string reasonPhrase)
            : this(message, statusCode, reasonPhrase, debugResponse: null, innerException: null)
        {
        }

        /// <summary>
        /// Initialize the exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="statusCode">The HTTP status code that caused this exception.</param>
        /// <param name="reasonPhrase">The reason phrase returned with the <paramref name="statusCode"/>.</param>
        /// <param name="debugResponse">A string containing debug information about the response.</param>
        /// <param name="innerException">The inner exception.</param>
        public MiniZipHttpException(string message, HttpStatusCode statusCode, string reasonPhrase, string debugResponse, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            DebugResponse = debugResponse;
        }

        /// <summary>
        /// The HTTP status code of the response.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The HTTP reason phrase returned with the <see cref="StatusCode"/>.
        /// </summary>
        public string ReasonPhrase { get; }

        /// <summary>
        /// A string containing debug information about the response.
        /// </summary>
        public string DebugResponse { get; }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message =>
            base.Message +
            Environment.NewLine +
            Environment.NewLine +
            "Debug response:" +
            Environment.NewLine +
            DebugResponse;
    }
}
