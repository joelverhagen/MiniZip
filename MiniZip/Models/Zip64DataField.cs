namespace Knapcode.MiniZip
{
    /// <summary>
    /// Zip64 information found both in the central directory header and in the local file header.
    /// APPNOTE.TXT: 4.5.3 -Zip64 Extended Information Extra Field (0x0001)
    /// </summary>
    public class Zip64DataField
    {
        /// <summary>
        /// The uncompressed size of the file.
        /// </summary>
        public ulong? UncompressedSize { get; set; }

        /// <summary>
        /// The compressed size of the file.
        /// </summary>
        public ulong? CompressedSize { get; set; }
    }
}
