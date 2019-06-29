namespace Knapcode.MiniZip
{
    /// <summary>
    /// Source:
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipConstants.cs
    /// </summary>
    public static class ZipConstants
    {
        /// <summary>
        /// The size of the end of central record, excluding variable fields.
        /// </summary>
        public const int EndOfCentralDirectorySize = 22;

        /// <summary>
        /// The size of the end of central record, excluding the <see cref="EndOfCentralDirectorySignature"/> and variable
        /// fields.
        /// </summary>
        public const int EndOfCentralDirectorySizeWithoutSignature = EndOfCentralDirectorySize - sizeof(uint);

        /// <summary>
        /// The size of the local file header record, excluding variable fields.
        /// </summary>
        public const int LocalFileHeaderSize = 30;

        /// <summary>
        /// The size of the local file header record, excluding the <see cref="LocalFileHeaderSignature"/> variable
        /// fields.
        /// </summary>
        public const int LocalFileHeaderSizeWithoutSignature = LocalFileHeaderSize - sizeof(uint);

        /// <summary>
        /// The size of the data descriptor, including the optional <see cref="DataDescriptorSignature"/>.
        /// </summary>
        public const int DataDescriptorSize = 16;

        /// <summary>
        /// The size of the data descriptor, excluding the <see cref="DataDescriptorSignature"/>.
        /// </summary>
        public const int DataDescriptorSizeWithoutSignature = DataDescriptorSize - sizeof(uint);

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
        /// The signature for the end of central directory record.
        /// </summary>
        public const uint LocalFileHeaderSignature = 'P' | ('K' << 8) | (3 << 16) | (4 << 24);

        /// <summary>
        /// The optional signature for the data descriptor.
        /// </summary>
        public const uint DataDescriptorSignature = 'P' | ('K' << 8) | (7 << 16) | (8 << 24);

        /// <summary>
        /// The header ID for the Zip64 extended information extra field.
        /// </summary>
        public const ushort Zip64DataFieldHeaderId = 0x0001;

        /// <summary>
        /// The maximum size of the Zip64 extended information extra field in the central directory header.
        /// </summary>
        public const ushort MaximumZip64CentralDirectoryDataFieldSize = 28;

        /// <summary>
        /// The maximum size of the Zip64 extended information extra field in the local file header.
        /// </summary>
        public const ushort MaximumZip64LocalFileDataFieldSize = 16;

        /// <summary>
        /// The size of the Zip64 end of central directory locator.
        /// </summary>
        public const int Zip64EndOfCentralDirectoryLocatorSize = 20;

        /// <summary>
        /// The size of the Zip64 end of central directory locator.
        /// </summary>
        public const int Zip64EndOfCentralDirectoryLocatorSizeWithoutSignature = Zip64EndOfCentralDirectoryLocatorSize - sizeof(uint);

        /// <summary>
        /// The size of the Zip64 end of central record, excluding the <see cref="Zip64EndOfCentralDirectorySignature"/>
        /// and variable fields.
        /// </summary>
        public const int Zip64EndOfCentralDirectorySizeWithoutSignature = 56 - sizeof(uint);

        /// <summary>
        /// The size of the central directory entry header, excluding the <see cref="CentralDirectoryEntryHeaderSignature"/>
        /// and variable fields.
        /// </summary>
        public static int CentralDirectoryEntryHeaderSizeWithoutSignature = 46 - sizeof(uint);
    }
}
