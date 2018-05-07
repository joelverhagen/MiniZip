namespace Knapcode.MiniZip
{
    /// <summary>
    /// Known values for the <see cref="FileData.Flags"/>.
    /// APPNOTE.TXT: 4.4.4 general purpose bit flag
    /// </summary>
    public enum ZipEntryFlags : ushort
    {
        /// <summary>
        /// If set, indicates that the file is encrypted.
        /// </summary>
        Encrypted = 1 << 0,

        /// <summary>
        /// If this bit is set, the fields crc-32, compressed size and uncompressed size are set to zero in the local
        /// header. The correct values are put in the data descriptor immediately following the compressed data.
        /// </summary>
        DataDescriptor = 1 << 3,

        /// <summary>
        /// Language encoding flag (EFS).  If this bit is set, the filename and comment fields for this file MUST be
        /// encoded using UTF-8.
        /// </summary>
        UTF8 = 1 << 11,
    }
}
