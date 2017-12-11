namespace Knapcode.MiniZip
{
    public class ZipDataField
    {
        public ushort HeaderId { get; set; }
        public ushort DataSize { get; set; }
        public byte[] Data { get; set; }
    }
}
