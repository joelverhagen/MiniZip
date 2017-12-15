namespace Knapcode.MiniZip
{
    /// <summary>
    /// A data field in the ZIP entry.
    /// APPNOTE.TXT: 4.5 Extensible data fields
    /// </summary>
    public class ZipDataField
    {
        /// <summary>
        /// An identifier for the type of data field.
        /// </summary>
        public ushort HeaderId { get; set; }

        /// <summary>
        /// The size of the data.
        /// </summary>
        public ushort DataSize { get; set; }

        /// <summary>
        /// The data itself.
        /// </summary>
        public byte[] Data { get; set; }
    }
}
