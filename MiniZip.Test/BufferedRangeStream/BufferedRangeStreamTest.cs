using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Knapcode.MiniZip
{
    public class BufferedRangeStreamTest
    {
        public class ReadAsync
        {
            private readonly byte[] _buffer;
            private readonly Mock<BufferRangeReader> _rangeReader;
            private readonly int _length;
            private readonly Mock<IBufferSizeProvider> _bufferSizeProvider;
            private readonly BufferedRangeStream _target;
            private readonly byte[] _outputBuffer;
            private readonly byte[] _extraBuffer;
            private int _read;

            public ReadAsync()
            {
                _buffer = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
                _rangeReader = new Mock<BufferRangeReader>(_buffer) { CallBase = true };
                _length = _buffer.Length;
                _bufferSizeProvider = new Mock<IBufferSizeProvider>();

                _bufferSizeProvider
                    .Setup(x => x.GetNextBufferSize())
                    .Returns(10);

                _target = new BufferedRangeStream(
                    _rangeReader.Object,
                    _length,
                    _bufferSizeProvider.Object);

                _target.Position = 80;

                _outputBuffer = new byte[_length];
                _extraBuffer = new byte[_length];
            }

            [Fact]
            public async Task JoinsBuffersFromOverlappingReads()
            {
                // Arrange
                await _target.ReadAsync(_outputBuffer, 0, 5);
                _target.Position = 73;
                _rangeReader.ResetCalls();

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 5);

                // Assert
                VerifyOutputBuffer(73, 5);
                VerifyReads(73, 7);
                await VerifyInternalBufferAsync(73, 17);
            }

            [Fact]
            public async Task JoinsBuffersFromFittingNextToEachOther()
            {
                // Arrange
                await _target.ReadAsync(_outputBuffer, 0, 5);
                _target.Position = 70;
                _rangeReader.ResetCalls();

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 5);

                // Assert
                VerifyOutputBuffer(70, 5);
                VerifyReads(70, 10);
                await VerifyInternalBufferAsync(70, 20);
            }

            [Fact]
            public async Task FillsGapBetweenTwoBuffers()
            {
                // Arrange
                await _target.ReadAsync(_outputBuffer, 0, 5);
                _target.Position = 65;
                _rangeReader.ResetCalls();

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 5);

                // Assert
                VerifyOutputBuffer(65, 5);
                VerifyReads(65, 15);
                await VerifyInternalBufferAsync(65, 25);
            }

            [Fact]
            public async Task ReadsExtraBehindPositionIfProviderBufferSizeIsLargerThanAvailable()
            {
                // Arrange
                _bufferSizeProvider
                    .Setup(x => x.GetNextBufferSize())
                    .Returns(25);

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 5);

                // Assert
                VerifyOutputBuffer(80, 5);
                VerifyReads(75, 25);
                await VerifyInternalBufferAsync(75, 25);
            }

            [Fact]
            public async Task ReadsEntireStreamIfProviderBufferSizeIsLargerThanLength()
            {
                // Arrange
                _bufferSizeProvider
                    .Setup(x => x.GetNextBufferSize())
                    .Returns(101);

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 5);

                // Assert
                VerifyOutputBuffer(80, 5);
                VerifyReads(0, 100);
                await VerifyInternalBufferAsync(0, 100);
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
                Assert.Equal(-1, _target.BufferPosition);
                Assert.Equal(0, _target.BufferSize);
            }

            [Fact]
            public async Task UsesProviderBufferSizeIfLarger()
            {
                // Arrange & Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 5);

                // Assert
                VerifyOutputBuffer(80, 5);
                VerifyReads(80, 10);
                await VerifyInternalBufferAsync(80, 10);
            }

            [Fact]
            public async Task ReadsCountIfLargerThanBufferSize()
            {
                // Arrange
                _bufferSizeProvider
                    .Setup(x => x.GetNextBufferSize())
                    .Returns(5);

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 10);

                // Assert
                VerifyOutputBuffer(80, 10);
                VerifyReads(80, 10);
                await VerifyInternalBufferAsync(80, 10);
            }

            [Theory]
            [InlineData(-2)]
            [InlineData(0)]
            [InlineData(2)]
            public async Task DoesNotAllowReadingAfterTheBuffer(int delta)
            {
                // Arrange
                await _target.ReadAsync(_extraBuffer, 0, 5);
                _target.Position = 90 + delta;
                _rangeReader.ResetCalls();

                // Act & Assert
                var exception = await Assert.ThrowsAsync<NotSupportedException>(
                    () => _target.ReadAsync(_outputBuffer, 0, 5));
                Assert.Contains("Reading past the end of the buffer is not supported.", exception.Message);
                VerifyUntouchedOutputBuffer();
                VerifyNoReads();
                await VerifyInternalBufferAsync(80, 10);
            }

            [Theory]
            [InlineData(-5)]
            [InlineData(-4)]
            [InlineData(-3)]
            [InlineData(-2)]
            [InlineData(-1)]
            [InlineData(0)]
            public async Task AllowsReadingAfterOldPositionInBuffer(int delta)
            {
                // Arrange
                await _target.ReadAsync(_extraBuffer, 0, 5);
                _target.Position = 85 + delta;
                _rangeReader.ResetCalls();

                // Act
                _read = await _target.ReadAsync(_outputBuffer, 0, 5);

                // Assert
                VerifyOutputBuffer(85 + delta, 5);
                VerifyNoReads();
                await VerifyInternalBufferAsync(80, 10);
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

            private async Task VerifyInternalBufferAsync(int position, int size)
            {
                Assert.Equal(position, _target.BufferPosition);
                Assert.Equal(size, _target.BufferSize);
                var bufferCopy = new byte[size];
                _target.Position = position;
                var read = await _target.ReadAsync(bufferCopy, 0, size);
                Assert.Equal(size, read);
                Assert.Equal(GetBytes(position, size), bufferCopy);

                _rangeReader.ResetCalls();
                _rangeReader.Verify(
                    x => x.ReadAsync(It.IsAny<long>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()),
                    Times.Never);
            }

            private static byte[] GetBytes(int start, int count)
            {
                return Enumerable.Range(start, count).Select(x => (byte)x).ToArray();
            }
        }
    }
}
