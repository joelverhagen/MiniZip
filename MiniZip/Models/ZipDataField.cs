using System;

namespace Knapcode.MiniZip
{
    public class ZipDataField : IEquatable<ZipDataField>
    {
        public ushort HeaderId { get; set; }
        public ushort DataSize { get; set; }
        public byte[] Data { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ZipDataField);
        }

        public bool Equals(ZipDataField other)
        {
            return other != null &&
                   HeaderId == other.HeaderId &&
                   DataSize == other.DataSize &&
                   ListEqualityComparer<byte>.Default.Equals(Data, other.Data);
        }

        public override int GetHashCode()
        {
            var hashCode = -923723634;
            hashCode = hashCode * -1521134295 + HeaderId.GetHashCode();
            hashCode = hashCode * -1521134295 + DataSize.GetHashCode();
            hashCode = hashCode * -1521134295 + ListEqualityComparer<byte>.Default.GetHashCode(Data);
            return hashCode;
        }
    }
}
