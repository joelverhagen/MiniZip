using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Knapcode.MiniZip
{
    public class RangeStreamTest
    {
        public class ReadAsync
        {
            private readonly byte[] _buffer;
            private readonly Mock<BufferRangeReader> _rangeReader;
            private readonly int _length;
            private readonly RangeStream _target;
            private readonly byte[] _outputBuffer;
            private int _read;

            public ReadAsync()
            {
                _buffer = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
                _rangeReader = new Mock<BufferRangeReader>(_buffer) { CallBase = true };
                _length = _buffer.Length;

                _target = new RangeStream(_rangeReader.Object, _length);

                _target.Position = 80;

                _outputBuffer = new byte[_length];
            }

            [Theory]
            [InlineData(19, 19)]
            [InlineData(20, 20)]
            [InlineData(21, 20)]
            public async Task ReadsRequestedBytes(int requested, int read)
            {
                // Arrange & Act
                _read = await _target.ReadAsync(_outputBuffer, 0, requested);

                // Assert
                VerifyOutputBuffer(80, read);
                VerifyReads(80, read);
            }

            [Fact]
            public async Task ReturnsZeroIfZeroIsRequest()
            {
                // Arrange & Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 0);

                // Assert
                VerifyUntouchedOutputBuffer();
                VerifyNoReads();
            }

            [Fact]
            public async Task ReturnsZeroIfPositionIsAfterEnd()
            {
                // Arrange
                _target.Position = _length + 1;

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 1);

                // Assert
                VerifyUntouchedOutputBuffer();
                VerifyNoReads();
            }

            private void VerifyUntouchedOutputBuffer()
            {
                Assert.Equal(0, _read);
                Assert.Equal(
                    Enumerable.Repeat((byte)0, _length).ToArray(),
                    _outputBuffer);
            }

            private void VerifyOutputBuffer(int position, int count)
            {
                Assert.Equal(count, _read);
                Assert.Equal(GetBytes(position, count), _outputBuffer.Take(count).ToArray());
            }

            private void VerifyNoReads()
            {
                _rangeReader.Verify(
                    x => x.ReadAsync(It.IsAny<long>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()),
                    Times.Never);
            }

            private void VerifyReads(int position, int count)
            {
                _rangeReader.Verify(
                    x => x.ReadAsync(position, It.IsAny<byte[]>(), 0, count),
                    Times.Once);
                _rangeReader.Verify(
                    x => x.ReadAsync(It.IsAny<long>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()),
                    Times.Once);
            }

            private static byte[] GetBytes(int start, int count)
            {
                return Enumerable.Range(start, count).Select(x => (byte)x).ToArray();
            }
        }
    }
}
