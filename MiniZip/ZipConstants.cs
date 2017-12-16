namespace Knapcode.MiniZip
{
    /// <summary>
    /// Source:
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipConstants.cs
    /// </summary>
    public static class ZipConstants
    {
        /// <summary>
        /// Size of end of central record, excluding variable fields.
        /// </summary>
        public const int EndOfCentralDirectorySize = 22;

        /// <summary>
        /// The size of end of central record, excluding the <see cref="EndOfCentralDirectorySignature"/> and variable
        /// fields.
        /// </summary>
        public const int EndOfCentralDirectorySizeWithoutSignature = EndOfCentralDirectorySize - sizeof(uint);

        /// <summary>
        /// The signature for the central header.
        /// </summary>
        public const uint CentralDirectoryEntryHeaderSignature = 'P' | ('K' << 8) | (1 << 16) | (2 << 24);

        /// <summary>
        /// The signature for the Zip64 central file header.
        /// </summary>
        public const uint Zip64EndOfCentralDirectorySignature = 'P' | ('K' << 8) | (6 << 16) | (6 << 24);

        /// <summary>
        /// The signature for the Zip64 central directory locator.
        /// </summary>
        public const uint Zip64EndOfCentralDirectoryLocatorSignature = 'P' | ('K' << 8) | (6 << 16) | (7 << 24);
        
        /// <summary>
        /// The signature for the end of central directory record.
        /// </summary>
        public const uint EndOfCentralDirectorySignature = 'P' | ('K' << 8) | (5 << 16) | (6 << 24);

        /// <summary>
        /// The header ID for the Zip64 extended information extra field.
        /// </summary>
        public const ushort Zip64DataFieldHeaderId = 1;

        /// <summary>
        /// The size of the Zip64 extended information extra field.
        /// </summary>
        public const ushort MaximumZip64DataFieldSize = 28;

        /// <summary>
        /// The size of the Zip64 locator record, excluding the <see cref="Zip64EndOfCentralDirectoryLocatorSignature"/>.
        /// </summary>
        public const int Zip64EndOfCentralDirectoryLocatorSizeWithoutSignature = 20 - sizeof(uint);

        /// <summary>
        /// The size of the Zip64 end of central record, excluding the <see cref="Zip64EndOfCentralDirectorySignature"/>
        /// and variable fields.
        /// </summary>
        public const int Zip64EndOfCentralDirectorySizeWithoutSignature = 56 - sizeof(uint);

        /// <summary>
        /// The size of the central directory entry header, excluding the <see cref="CentralDirectoryEntryHeaderSignature"/>
        /// and variable fields.
        /// </summary>
        internal static int CentralDirectoryEntryHeaderSizeWithoutSignature = 46 - sizeof(uint);
    }
}
