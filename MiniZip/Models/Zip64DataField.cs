using System;
using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    public class Zip64DataField : IEquatable<Zip64DataField>
    {
        public ulong? UncompressedSize { get; set; }
        public ulong? CompressedSize { get; set; }
        public ulong? LocalHeaderOffset { get; set; }
        public ulong? DiskNumberStart { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Zip64DataField);
        }

        public bool Equals(Zip64DataField other)
        {
            return other != null &&
                   EqualityComparer<ulong?>.Default.Equals(UncompressedSize, other.UncompressedSize) &&
                   EqualityComparer<ulong?>.Default.Equals(CompressedSize, other.CompressedSize) &&
                   EqualityComparer<ulong?>.Default.Equals(LocalHeaderOffset, other.LocalHeaderOffset) &&
                   EqualityComparer<ulong?>.Default.Equals(DiskNumberStart, other.DiskNumberStart);
        }

        public override int GetHashCode()
        {
            var hashCode = -484200552;
            hashCode = hashCode * -1521134295 + EqualityComparer<ulong?>.Default.GetHashCode(UncompressedSize);
            hashCode = hashCode * -1521134295 + EqualityComparer<ulong?>.Default.GetHashCode(CompressedSize);
            hashCode = hashCode * -1521134295 + EqualityComparer<ulong?>.Default.GetHashCode(LocalHeaderOffset);
            hashCode = hashCode * -1521134295 + EqualityComparer<ulong?>.Default.GetHashCode(DiskNumberStart);
            return hashCode;
        }
    }
}
