namespace Knapcode.MiniZip
{
    /// <summary>
    /// Zip64 metadata about the directory.
    /// APPNOTE.TXT: 4.3.14 Zip64 end of central directory record
    /// </summary>
    public class Zip64Directory
    {
        /// <summary>
        /// The offset of the position after the Zip64 central directory locator.
        /// </summary>
        public long OffsetAfterEndOfCentralDirectoryLocator { get; set; }

        /// <summary>
        /// The number of the disk with the start of the Zip64 end of central directory.
        /// </summary>
        public uint DiskWithStartOfEndOfCentralDirectory { get; set; }

        /// <summary>
        /// The relative offset of the Zip64 end of central directory record.
        /// </summary>
        public ulong EndOfCentralDirectoryOffset { get; set; }

        /// <summary>
        /// The total number of disks.
        /// </summary>
        public uint TotalNumberOfDisks { get; set; }

        /// <summary>
        /// Size of zip64 end of central directory record.
        /// </summary>
        public ulong SizeOfCentralDirectoryRecord { get; set; }

        /// <summary>
        /// The version made by.
        /// </summary>
        public ushort VersionMadeBy { get; set; }

        /// <summary>
        /// The version needed to extract.
        /// </summary>
        public ushort VersionToExtract { get; set; }

        /// <summary>
        /// The number of this disk.
        /// </summary>
        public uint NumberOfThisDisk { get; set; }

        /// <summary>
        /// The number of the disk with the start of the central directory.
        /// </summary>
        public uint DiskWithStartOfCentralDirectory { get; set; }

        /// <summary>
        /// Total number of entries in the central directory on this disk.
        /// </summary>
        public ulong EntriesInThisDisk { get; set; }

        /// <summary>
        /// The total number of entries in the central directory.
        /// </summary>
        public ulong EntriesForWholeCentralDirectory { get; set; }

        /// <summary>
        /// The size of the central directory.
        /// </summary>
        public ulong CentralDirectorySize { get; set; }

        /// <summary>
        /// The offset of start of central directory with respect to the starting disk number.
        /// </summary>
        public ulong OffsetOfCentralDirectory { get; set; }
    }
}
