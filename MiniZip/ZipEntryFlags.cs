namespace Knapcode.MiniZip
{
    /// <summary>
    /// Known values for the <see cref="ZipEntry.Flags"/>.
    /// APPNOTE.TXT: 4.4.4 general purpose bit flag
    /// </summary>
    public enum ZipEntryFlags : ushort
    {
        /// <summary>
        /// Language encoding flag (EFS).  If this bit is set, the filename and comment fields for this file MUST be
        /// encoded using UTF-8.
        /// </summary>
        UTF8 = 1 << 11,
    }
}
