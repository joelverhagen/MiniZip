using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class HttpMessageExtensionTest
    {
        public class TheToHttpExceptionAsyncMethod
        {
            [Fact]
            public async Task PutsRequestAndResponseInExceptionMessage()
            {
                // Arrange
                var request = new HttpRequestMessage(HttpMethod.Put, "https://www.example.com/v3/index.json");
                request.Headers.TryAddWithoutValidation("User-Agent", "my fake UA");
                request.Content = new StringContent("my test content\nand the second line too.");
                request.Content.Headers.TryAddWithoutValidation("X-Content-Header", "I'm here too!");

                var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.ReasonPhrase = "Not good request";
                response.Headers.Age = TimeSpan.FromDays(9001);
                response.Content = new StringContent("This is the super cool response.");
                response.RequestMessage = request;

                // Act
                var exception = await response.ToHttpExceptionAsync("The request failed I guess?");

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
                Assert.Equal("Not good request", exception.ReasonPhrase);
                Assert.Null(exception.InnerException);
                var nl = Environment.NewLine;
                Assert.Equal(
                    $"The request failed I guess?{nl}{nl}=== Request ==={nl}PUT https://www.example.com/v3/index.json HTTP/1.1\r\nUser-Agent: my\r\nUser-Agent: fake\r\nUser-Agent: UA\r\nContent-Type: text/plain; charset=utf-8\r\nX-Content-Header: I'm here too!\r\n\r\nmy test content\nand the second line too.{nl}{nl}=== Response ==={nl}HTTP/1.1 400 Not good request\r\nAge: 777686400\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nThis is the super cool response.",
                    exception.Message);
            }
        }
    }
}
