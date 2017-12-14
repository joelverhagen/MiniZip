namespace Knapcode.MiniZip
{
    public class Zip64DataField
    {
        public ulong? UncompressedSize { get; set; }
        public ulong? CompressedSize { get; set; }
        public ulong? LocalHeaderOffset { get; set; }
        public ulong? DiskNumberStart { get; set; }
    }
}
