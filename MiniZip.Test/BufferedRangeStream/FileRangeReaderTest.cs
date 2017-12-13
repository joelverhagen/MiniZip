using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class FileRangeReaderTest
    {
        public class ReadAsync : IDisposable
        {
            private readonly TestDirectory _directory;
            private readonly string _path;
            private readonly byte[] _content;
            private readonly FileRangeReader _target;
            private readonly byte[] _outputBuffer;

            public ReadAsync()
            {
                _directory = TestDirectory.Create();
                _path = Path.Combine(_directory, "file.dat");
                _content = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
                File.WriteAllBytes(_path, _content);
                _target = new FileRangeReader(_path);
                _outputBuffer = new byte[_content.Length];
            }

            [Fact]
            public void DoesNotHaveHandleBeforeRead()
            {
                // Arrange & Act
                File.Delete(_path);

                // Assert
                Assert.False(File.Exists(_path));
            }

            [Fact]
            public async Task DoesNotHaveHandleAfterRead()
            {
                // Arrange
                await _target.ReadAsync(0, _outputBuffer, 0, 5);

                // Act
                File.Delete(_path);

                // Assert
                Assert.False(File.Exists(_path));
            }

            [Fact]
            public async Task ThrowsIfFileDoesNotExist()
            {
                // Arrange
                File.Delete(_path);

                // Act & Assert
                await Assert.ThrowsAsync<FileNotFoundException>(
                    () => _target.ReadAsync(0, _outputBuffer, 0, 5));
            }

            [Fact]
            public async Task ReadsCorrectRange()
            {
                // Arrange
                var expected = Enumerable
                    .Empty<byte>()
                    .Concat(Enumerable.Repeat((byte)0, 5))
                    .Concat(Enumerable.Range(25, 10).Select(x => (byte)x))
                    .Concat(Enumerable.Repeat((byte)0, 85))
                    .ToArray();

                // Act
                await _target.ReadAsync(25, _outputBuffer, 5, 10);

                // Assert
                Assert.Equal(expected, _outputBuffer);
            }

            [Fact]
            public async Task CanReadMultipleTimes()
            {
                // Arrange
                var expected = Enumerable
                    .Empty<byte>()
                    .Concat(Enumerable.Repeat((byte)0, 2))
                    .Concat(Enumerable.Range(7, 10).Select(x => (byte)x))
                    .Concat(Enumerable.Range(32, 3).Select(x => (byte)x))
                    .Concat(Enumerable.Repeat((byte)0, 85))
                    .ToArray();

                // Act
                await _target.ReadAsync(25, _outputBuffer, 5, 10);
                await _target.ReadAsync(7, _outputBuffer, 2, 10);

                // Assert
                Assert.Equal(expected, _outputBuffer);
            }

            public void Dispose()
            {
                _directory?.Dispose();
            }
        }
    }
}
