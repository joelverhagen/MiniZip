using System;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// A range reader which reads from a specific file path. This is ideal for testing the
    /// <see cref="BufferedRangeStream"/> as it requires a <see cref="IRangeReader"/>.
    /// </summary>
    public class FileRangeReader : SeekableStreamRangeReader
    {
        /// <summary>
        /// Initializes a file ranger reader which reads from the provided file path.
        /// </summary>
        /// <param name="path">The file to read from.</param>
        public FileRangeReader(string path) : base(GetOpenStreamAsync(path))
        {
        }

        private static Func<Task<Stream>> GetOpenStreamAsync(string path)
        {
            return () => Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read));
        }
    }
}
