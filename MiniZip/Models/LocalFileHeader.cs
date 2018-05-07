using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// The local file header information.
    /// </summary>
    public class LocalFileHeader : FileData
    {
        /// <summary>
        /// The data fields that are Zip64 extended information.
        /// </summary>
        public List<Zip64LocalFileDataField> Zip64DataFields { get; set; }

        /// <summary>
        /// The optional data descriptor found after the local file header.
        /// </summary>
        public DataDescriptor DataDescriptor { get; set; }
    }
}
