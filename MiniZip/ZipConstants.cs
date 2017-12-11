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
        public const int EndOfCentralRecordBaseSize = 22;

        /// <summary>
        /// Signature for central header.
        /// </summary>
        public const uint CentralHeaderSignature = 'P' | ('K' << 8) | (1 << 16) | (2 << 24);

        /// <summary>
        /// Signature for Zip64 central file header.
        /// </summary>
        public const uint Zip64CentralFileHeaderSignature = 'P' | ('K' << 8) | (6 << 16) | (6 << 24);

        /// <summary>
        /// Signature for Zip64 central directory locator.
        /// </summary>
        public const uint Zip64EndOfCentralDirectoryLocatorSignature = 'P' | ('K' << 8) | (6 << 16) | (7 << 24);

        /// <summary>
        /// Central header digitial signature.
        /// </summary>
        public const uint CentralHeaderDigitalSignature = 'P' | ('K' << 8) | (5 << 16) | (5 << 24);

        /// <summary>
        /// End of central directory record signature.
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
    }
}
