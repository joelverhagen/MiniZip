using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Knapcode.MiniZip
{
    public class BlockMemoryStreamTest
    {
        public class Read
        {
            [Theory]
            [MemberData(nameof(ReadsExpectedBytesData))]
            public void ReadsExpectedBytes(int position)
            {
                // Arrange
                var bufferA = Encoding.ASCII.GetBytes("0");
                var bufferB = Encoding.ASCII.GetBytes("12");
                var bufferC = Encoding.ASCII.GetBytes("345");
                var bufferD = Encoding.ASCII.GetBytes("6789");
                var bufferE = Encoding.ASCII.GetBytes("ABCDE");

                var target = new BlockMemoryStream();

                target.Append(bufferC);
                target.Prepend(bufferB);
                target.Append(bufferD);
                target.Append(bufferE);
                target.Prepend(bufferA);

                var expected = "0123456789ABCDE" + new string('_', (int)target.Length);
                expected = expected.Substring(position);
                var buffer = Encoding.ASCII.GetBytes(new string('_', expected.Length));

                target.Position = position;

                // Act
                var read = target.Read(buffer, 0, buffer.Length);

                // Assert
                var actual = Encoding.ASCII.GetString(buffer);
                Assert.Equal(expected, actual);
                Assert.Equal(Math.Max(0, target.Length - position), read);
            }

            public static IEnumerable<object[]> ReadsExpectedBytesData => Enumerable
                .Range(0, 15 + 4)
                .Select(x => new object[] { x });
        }
    }
}
