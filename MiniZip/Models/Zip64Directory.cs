using System;

namespace Knapcode.MiniZip
{
    public class Zip64Directory : IEquatable<Zip64Directory>
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

        public override bool Equals(object obj)
        {
            return Equals(obj as Zip64Directory);
        }

        public bool Equals(Zip64Directory other)
        {
            return other != null &&
                   OffsetAfterEndOfCentralDirectoryLocator == other.OffsetAfterEndOfCentralDirectoryLocator &&
                   DiskWithStartOfEndOfCentralDirectory == other.DiskWithStartOfEndOfCentralDirectory &&
                   EndOfCentralDirectoryOffset == other.EndOfCentralDirectoryOffset &&
                   TotalNumberOfDisks == other.TotalNumberOfDisks &&
                   SizeOfCentralDirectoryRecord == other.SizeOfCentralDirectoryRecord &&
                   VersionMadeBy == other.VersionMadeBy &&
                   VersionToExtract == other.VersionToExtract &&
                   NumberOfThisDisk == other.NumberOfThisDisk &&
                   DiskWithStartOfCentralDirectory == other.DiskWithStartOfCentralDirectory &&
                   EntriesInThisDisk == other.EntriesInThisDisk &&
                   EntriesForWholeCentralDirectory == other.EntriesForWholeCentralDirectory &&
                   CentralDirectorySize == other.CentralDirectorySize &&
                   OffsetOfCentralDirectory == other.OffsetOfCentralDirectory;
        }

        public override int GetHashCode()
        {
            var hashCode = 671837334;
            hashCode = hashCode * -1521134295 + OffsetAfterEndOfCentralDirectoryLocator.GetHashCode();
            hashCode = hashCode * -1521134295 + DiskWithStartOfEndOfCentralDirectory.GetHashCode();
            hashCode = hashCode * -1521134295 + EndOfCentralDirectoryOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + TotalNumberOfDisks.GetHashCode();
            hashCode = hashCode * -1521134295 + SizeOfCentralDirectoryRecord.GetHashCode();
            hashCode = hashCode * -1521134295 + VersionMadeBy.GetHashCode();
            hashCode = hashCode * -1521134295 + VersionToExtract.GetHashCode();
            hashCode = hashCode * -1521134295 + NumberOfThisDisk.GetHashCode();
            hashCode = hashCode * -1521134295 + DiskWithStartOfCentralDirectory.GetHashCode();
            hashCode = hashCode * -1521134295 + EntriesInThisDisk.GetHashCode();
            hashCode = hashCode * -1521134295 + EntriesForWholeCentralDirectory.GetHashCode();
            hashCode = hashCode * -1521134295 + CentralDirectorySize.GetHashCode();
            hashCode = hashCode * -1521134295 + OffsetOfCentralDirectory.GetHashCode();
            return hashCode;
        }
    }
}
