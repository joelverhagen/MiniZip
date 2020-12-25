using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An interface for reading and writing the custom "MZip" format. This format is simply a ZIP file's central
    /// directory prefixed with the central directory's offset in the original ZIP file. This is useful for when
    /// only the metadata about the ZIP file (none of the file contents) are to be persisted somewhere.
    /// </summary>
    public class MZipFormat : IMZipFormat
    {
        /// <summary>
        /// Writes the central directory of the ZIP file in the <paramref name="srcStream"/> to the 
        /// <paramref name="dstStream"/>. The <paramref name="dstStream"/> should be read using
        /// <see cref="ReadAsync(Stream)"/>.
        /// </summary>
        /// <param name="srcStream">The stream to be read which contains a ZIP file.</param>
        /// <param name="dstStream">The stream to be written to which will contain the central directory.</param>
        public async Task WriteAsync(Stream srcStream, Stream dstStream)
        {
            using (var reader = new ZipDirectoryReader(srcStream, leaveOpen: true))
            {
                var zipDirectory = await reader.ReadAsync();

                // First, write the offset of the central directory.
                var position = (long)(zipDirectory.Zip64?.OffsetOfCentralDirectory ?? zipDirectory.OffsetOfCentralDirectory);
                using (var binaryWriter = new BinaryWriter(dstStream, Encoding.ASCII, leaveOpen: true))
                {
                    binaryWriter.Write((ulong)position);
                }

                // Next, write the central directory itself.
                srcStream.Position = position;
                await srcStream.CopyToAsync(dstStream);
            }
        }

        /// <summary>
        /// Wraps the provided <paramref name="srcStream"/> (which should be a stream written by
        /// <see cref="WriteAsync(Stream, Stream)"/>) in a virtual stream readable by most ZIP archive readers. For
        /// example, you can use <see cref="ZipDirectoryReader"/> to read the returned stream. The returned stream does
        /// not contain ZIP file entry contents and only contains the central directory.
        /// </summary>
        /// <param name="srcStream">The stream containing the "MZIP" format.</param>
        /// <returns>A stream readable as a ZIP archive with a central directory but no entries.</returns>
        public Task<Stream> ReadAsync(Stream srcStream)
        {
            using (var binaryReader = new BinaryReader(srcStream, Encoding.ASCII, leaveOpen: true))
            {
                // First, read the offset of the central directory.
                var virtualOffset = (long)binaryReader.ReadUInt64();

                // Next, wrap the rest of the stream as if it was at the end of the full ZIP file.
                var boundedStream = new BoundedStream(srcStream, srcStream.Position, srcStream.Length - 1);
                var outputStream = new VirtualOffsetStream(boundedStream, virtualOffset);
                return Task.FromResult<Stream>(outputStream);
            }
        }
    }
}
