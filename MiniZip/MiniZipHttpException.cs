using System;
using System.Net;
using System.Text;

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
            : this(message, debugRequest: null, statusCode, reasonPhrase, debugResponse, innerException)
        {
        }

        /// <summary>
        /// Initialize the exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="debugRequest">A string containing debug information about the request.</param>
        /// <param name="statusCode">The HTTP status code that caused this exception.</param>
        /// <param name="reasonPhrase">The reason phrase returned with the <paramref name="statusCode"/>.</param>
        /// <param name="debugResponse">A string containing debug information about the response.</param>
        /// <param name="innerException">The inner exception.</param>
        public MiniZipHttpException(string message, string debugRequest, HttpStatusCode statusCode, string reasonPhrase, string debugResponse, Exception innerException)
            : base(message, innerException)
        {
            DebugRequest = debugRequest;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            DebugResponse = debugResponse;
        }

        /// <summary>
        /// A string containing debug information about the request.
        /// </summary>
        public string DebugRequest { get; }

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
        public override string Message
        {
            get
            {
                if (DebugRequest == null && DebugResponse == null)
                {
                    return base.Message;
                }

                var sb = new StringBuilder();
                sb.AppendLine(base.Message);
                sb.AppendLine();

                if (DebugRequest != null)
                {
                    sb.AppendLine("=== Request ===");
                    sb.Append(DebugRequest);
                    if (DebugResponse != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                }

                if (DebugResponse != null)
                {
                    sb.AppendLine("=== Response ===");
                    sb.Append(DebugResponse);
                }

                return sb.ToString();
            }
        }
    }
}
