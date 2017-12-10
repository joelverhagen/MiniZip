namespace Knapcode.MiniZip
{
    public class Zip64Directory
    {
        public long OffsetAfterEndOfCentralDirectoryLocator { get; set; }
        public uint DiskWithStartOfEndOfCentralDirectory { get; set; }
        public ulong EndOfCentralDirectoryOffset { get; set; }
        public uint TotalNumberOfDisks { get; set; }
        public ulong SizeOfCentralDirectoryRecord { get; set; }
        public ushort VersionMadeBy { get; set; }
        public ushort VersionToExtract { get; set; }
        public uint NumberOfThisDisk { get; set; }
        public uint DiskWithStartOfCentralDirectory { get; set; }
        public ulong EntriesInThisDisk { get; set; }
        public ulong EntriesForWholeCentralDirectory { get; set; }
        public ulong CentralDirectorySize { get; set; }
        public ulong OffsetOfCentralDirectory { get; set; }
    }
}
