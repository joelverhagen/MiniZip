namespace Knapcode.MiniZip
{
    /// <summary>
    /// Known values for the <see cref="FileData.CompressionMethod"/>.
    /// APPNOTE.TXT: 4.4.5 compression method
    /// </summary>
    public enum ZipCompressionMethod : ushort
    {
        /// <summary>
        /// The file is stored (no compression)
        /// </summary>
        Store = 0,
    }
}
