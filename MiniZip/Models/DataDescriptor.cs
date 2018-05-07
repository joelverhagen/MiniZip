namespace Knapcode.MiniZip
{
    /// <summary>
    /// Fields used by a file data descriptor.
    /// </summary>
    public class DataDescriptor
    {
        /// <summary>
        /// Whether or not the data descriptor has the optional <see cref="ZipConstants.DataDescriptorSignature"/>.
        /// </summary>
        public bool HasSignature { get; set; }

        /// <summary>
        /// The CRC-32 of the file.
        /// </summary>
        public uint Crc32 { get; set; }

        /// <summary>
        /// The compressed size of the file.
        /// </summary>
        public uint CompressedSize { get; set; }

        /// <summary>
        /// The uncompressed size of the file.
        /// </summary>
        public uint UncompressedSize { get; set; }
    }
}
