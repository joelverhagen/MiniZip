using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Reads a stream containing a ZIP archive file. The stream operated on must support reading, seeking, and must
    /// have a known length.
    /// </summary>
    /// <remarks>
    /// Based off of:
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipFile.cs
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipHelperStream.cs
    /// </remarks>
    public class ZipDirectoryReader : IZipDirectoryReader
    {
        private static readonly int BufferSize = new[]
        {
            ZipConstants.EndOfCentralDirectorySizeWithoutSignature,
            ZipConstants.Zip64EndOfCentralDirectoryLocatorSizeWithoutSignature,
            ZipConstants.Zip64EndOfCentralDirectorySizeWithoutSignature,
            ZipConstants.CentralDirectoryEntryHeaderSizeWithoutSignature,
            sizeof(uint),
        }.Max();

        private Stream _stream;
        private readonly byte[] _buffer;
        private readonly bool _leaveOpen;
        private int _disposed;

        /// <summary>
        /// Initializes a ZIP directory reader, which reads the provided <see cref="Stream"/>. When this instance is
        /// disposed, the provided stream is also disposed.
        /// </summary>
        /// <param name="stream">The stream containing the ZIP archive.</param>
        public ZipDirectoryReader(Stream stream) : this(stream, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a ZIP directory reader, which reads the provided <see cref="Stream"/>. Whether or not the
        /// provided stream is disposed when this instance is disposed is controlled by <paramref name="leaveOpen"/>.
        /// </summary>
        /// <param name="stream">The stream containing the ZIP archive.</param>
        /// <param name="leaveOpen">Whether or not to leave the stream open when this instance is disposed.</param>
        public ZipDirectoryReader(Stream stream, bool leaveOpen)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _buffer = new byte[BufferSize];
            _leaveOpen = leaveOpen;
            _disposed = 0;

            if (!stream.CanSeek)
            {
                throw new ArgumentException(Strings.StreamMustSupportSeek, nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(Strings.StreamMustSupportRead, nameof(stream));
            }
        }

        /// <summary>
        /// Dispose this instance.
        /// </summary>
        public void Dispose()
        {
            var disposed = Interlocked.CompareExchange(ref _disposed, 1, 0);
            if (disposed == 0)
            {
                if (!_leaveOpen)
                {
                    _stream?.Dispose();
                }

                _stream = null;

            }
        }

        /// <summary>
        /// Whether or not this instance is disposed.
        /// </summary>
        public bool IsDisposed => _disposed != 0;

        /// <summary>
        /// Read the stream and gather all of the ZIP directory metadata. This extracts ZIP entry information and
        /// relative offsets. This method does not read the file entry contents or decompress anything. This method
        /// also does not decrypt anything.
        /// </summary>
        /// <returns>The ZIP directory metadata.</returns>
        public async Task<ZipDirectory> ReadAsync()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ZipDirectoryReader));
            }

            var zip = new ZipDirectory();

            zip.OffsetAfterEndOfCentralDirectory = await LocateBlockWithSignatureAsync(
                ZipConstants.EndOfCentralDirectorySignature,
                _stream.Length,
                ZipConstants.EndOfCentralDirectorySize,
                0xffff);

            if (zip.OffsetAfterEndOfCentralDirectory < 0)
            {
                throw new MiniZipException(Strings.CannotFindCentralDirectory);
            }

            using (var reader = await LoadBinaryReaderAsync(_buffer, ZipConstants.EndOfCentralDirectorySizeWithoutSignature))
            {
                zip.NumberOfThisDisk = reader.ReadUInt16();
                zip.DiskWithStartOfCentralDirectory = reader.ReadUInt16();
                zip.EntriesInThisDisk = reader.ReadUInt16();
                zip.EntriesForWholeCentralDirectory = reader.ReadUInt16();
                zip.CentralDirectorySize = reader.ReadUInt32();
                zip.OffsetOfCentralDirectory = reader.ReadUInt32();
                zip.CommentSize = reader.ReadUInt16();
            }
            
            zip.Comment = new byte[zip.CommentSize];
            await ReadFullyAsync(zip.Comment);

            // Check if the archive is Zip64.
            long offsetOfCentralDirectory;
            ulong entriesInThisDisk;
            if (zip.NumberOfThisDisk == 0xffff
                || zip.DiskWithStartOfCentralDirectory == 0xffff
                || zip.EntriesInThisDisk == 0xffff
                || zip.EntriesForWholeCentralDirectory == 0xffff
                || zip.CentralDirectorySize == 0xffffffff
                || zip.OffsetOfCentralDirectory == 0xffffffff)
            {
                zip.Zip64 = new Zip64Directory();

                zip.Zip64.OffsetAfterEndOfCentralDirectoryLocator = await LocateBlockWithSignatureAsync(
                    ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature,
                    zip.OffsetAfterEndOfCentralDirectory,
                    0,
                    0x1000);

                if (zip.Zip64.OffsetAfterEndOfCentralDirectoryLocator < 0)
                {
                    throw new MiniZipException(Strings.CannotFindZip64Locator);
                }

                using (var buffer = await LoadBinaryReaderAsync(_buffer, ZipConstants.Zip64EndOfCentralDirectoryLocatorSizeWithoutSignature))
                {
                    zip.Zip64.DiskWithStartOfEndOfCentralDirectory = buffer.ReadUInt32();
                    zip.Zip64.EndOfCentralDirectoryOffset = buffer.ReadUInt64();
                    zip.Zip64.TotalNumberOfDisks = buffer.ReadUInt32();
                }

                _stream.Position = (long)zip.Zip64.EndOfCentralDirectoryOffset;

                if (await ReadUInt32Async() != ZipConstants.Zip64EndOfCentralDirectorySignature)
                {
                    throw new MiniZipException(Strings.InvalidZip64CentralDirectorySignature);
                }

                using (var buffer = await LoadBinaryReaderAsync(_buffer, ZipConstants.Zip64EndOfCentralDirectorySizeWithoutSignature))
                {
                    zip.Zip64.SizeOfCentralDirectoryRecord = buffer.ReadUInt64();
                    zip.Zip64.VersionMadeBy = buffer.ReadUInt16();
                    zip.Zip64.VersionToExtract = buffer.ReadUInt16();
                    zip.Zip64.NumberOfThisDisk = buffer.ReadUInt32();
                    zip.Zip64.DiskWithStartOfCentralDirectory = buffer.ReadUInt32();
                    zip.Zip64.EntriesInThisDisk = buffer.ReadUInt64();
                    zip.Zip64.EntriesForWholeCentralDirectory = buffer.ReadUInt64();
                    zip.Zip64.CentralDirectorySize = buffer.ReadUInt64();
                    zip.Zip64.OffsetOfCentralDirectory = buffer.ReadUInt64();
                }

                offsetOfCentralDirectory = (long)zip.Zip64.OffsetOfCentralDirectory;
                entriesInThisDisk = zip.Zip64.EntriesInThisDisk;

                if ((zip.Zip64.NumberOfThisDisk != zip.NumberOfThisDisk && zip.NumberOfThisDisk != 0xffff)
                    || (zip.Zip64.DiskWithStartOfCentralDirectory != zip.DiskWithStartOfCentralDirectory && zip.DiskWithStartOfCentralDirectory != 0xffff)
                    || (zip.Zip64.EntriesInThisDisk != zip.EntriesInThisDisk && zip.EntriesInThisDisk != 0xffff)
                    || (zip.Zip64.EntriesForWholeCentralDirectory != zip.EntriesForWholeCentralDirectory && zip.EntriesForWholeCentralDirectory != 0xffff)
                    || (zip.Zip64.CentralDirectorySize != zip.CentralDirectorySize && zip.CentralDirectorySize != 0xffffffff)
                    || (zip.Zip64.OffsetOfCentralDirectory != zip.OffsetOfCentralDirectory && zip.OffsetOfCentralDirectory != 0xffffffff))
                {
                    throw new MiniZipException(Strings.InconsistentZip64Metadata);
                }
            }
            else
            {
                offsetOfCentralDirectory = zip.OffsetOfCentralDirectory;
                entriesInThisDisk = zip.EntriesInThisDisk;
            }

            _stream.Seek(offsetOfCentralDirectory, SeekOrigin.Begin);

            zip.Entries = new List<ZipEntry>();
            for (ulong i = 0; i < entriesInThisDisk; i++)
            {
                zip.Entries.Add(await ReadEntryAsync());
            }

            return zip;
        }

        private async Task<ZipEntry> ReadEntryAsync()
        {
            if (await ReadUInt32Async() != ZipConstants.CentralDirectoryEntryHeaderSignature)
            {
                throw new MiniZipException(Strings.InvalidCentralDirectorySignature);
            }

            var entry = new ZipEntry();

            using (var buffer = await LoadBinaryReaderAsync(_buffer, ZipConstants.CentralDirectoryEntryHeaderSizeWithoutSignature))
            {
                entry.VersionMadeBy = buffer.ReadUInt16();
                entry.VersionToExtract = buffer.ReadUInt16();
                entry.Flags = buffer.ReadUInt16();
                entry.CompressionMethod = buffer.ReadUInt16();
                entry.LastModifiedTime = buffer.ReadUInt16();
                entry.LastModifiedDate = buffer.ReadUInt16();
                entry.Crc32 = buffer.ReadUInt32();
                entry.CompressedSize = buffer.ReadUInt32();
                entry.UncompressedSize = buffer.ReadUInt32();
                entry.NameSize = buffer.ReadUInt16();
                entry.ExtraFieldSize = buffer.ReadUInt16();
                entry.CommentSize = buffer.ReadUInt16();
                entry.DiskNumberStart = buffer.ReadUInt16();
                entry.InternalAttributes = buffer.ReadUInt16();
                entry.ExternalAttributes = buffer.ReadUInt32();
                entry.LocalHeaderOffset = buffer.ReadUInt32();
            }

            entry.Name = new byte[entry.NameSize];
            await ReadFullyAsync(entry.Name);

            entry.ExtraField = new byte[entry.ExtraFieldSize];
            await ReadFullyAsync(entry.ExtraField);

            entry.DataFields = ReadDataFields(entry.ExtraField);
            entry.Zip64DataFields = ReadZip64DataFields(entry);

            entry.Comment = new byte[entry.CommentSize];
            await ReadFullyAsync(entry.Comment);

            return entry;
        }

        private List<ZipDataField> ReadDataFields(byte[] extraField)
        {
            var dataFields = new List<ZipDataField>();
            using (var reader = GetBinaryReader(extraField))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var dataField = new ZipDataField();

                    dataField.HeaderId = reader.ReadUInt16();
                    dataField.DataSize = reader.ReadUInt16();

                    dataField.Data = reader.ReadBytes(dataField.DataSize);
                    if (dataField.Data.Length < dataField.DataSize)
                    {
                        throw new EndOfStreamException();
                    }

                    dataFields.Add(dataField);
                }
            }

            return dataFields;
        }

        private List<Zip64DataField> ReadZip64DataFields(ZipEntry entry)
        {
            // Zip64 extended information extra field
            var zip64DataFields = new List<Zip64DataField>();
            foreach (var dataField in entry.DataFields)
            {
                if (dataField.HeaderId != ZipConstants.Zip64DataFieldHeaderId)
                {
                    continue;
                }

                if (dataField.DataSize > ZipConstants.MaximumZip64DataFieldSize)
                {
                    throw new MiniZipException(Strings.InvalidZip64ExtendedInformationLength);
                }

                using (var reader = GetBinaryReader(dataField.Data))
                {
                    var field = new Zip64DataField();

                    if (entry.UncompressedSize == 0xffffffff)
                    {
                        field.UncompressedSize = reader.ReadUInt64();
                    }

                    if (entry.CompressedSize == 0xffffffff)
                    {
                        field.CompressedSize = reader.ReadUInt64();
                    }

                    if (entry.LocalHeaderOffset == 0xffffffff)
                    {
                        field.LocalHeaderOffset = reader.ReadUInt64();
                    }

                    if (entry.DiskNumberStart == 0xffff)
                    {
                        field.DiskNumberStart = reader.ReadUInt32();
                    }

                    if (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        throw new MiniZipException(Strings.NotAllZip64ExtendedInformationWasRead);
                    }

                    zip64DataFields.Add(field);
                }
            }

            return zip64DataFields;
        }

        private async Task<long> LocateBlockWithSignatureAsync(
            uint signature,
            long endLocation,
            int minimumBlockSize,
            int maximumVariableData)
        {
            var pos = endLocation - minimumBlockSize;
            if (pos < 0)
            {
                return -1;
            }

            var giveUpMarker = Math.Max(pos - maximumVariableData, 0);

            do
            {
                if (pos < giveUpMarker)
                {
                    return -1;
                }

                _stream.Seek(pos--, SeekOrigin.Begin);
            }
            while (await ReadUInt32Async() != signature);

            return _stream.Position;
        }
        
        private async Task ReadFullyAsync(byte[] buffer)
        {
            await _stream.ReadExactlyAsync(buffer, buffer.Length);
        }

        private async Task<BinaryReader> LoadBinaryReaderAsync(byte[] buffer, int count)
        {
            await _stream.ReadExactlyAsync(buffer, count);
            return GetBinaryReader(buffer, count);
        }

        private static BinaryReader GetBinaryReader(byte[] buffer)
        {
            return GetBinaryReader(buffer, buffer.Length);
        }

        private static BinaryReader GetBinaryReader(byte[] buffer, int length)
        {
            var stream = new MemoryStream(buffer);
            stream.SetLength(length);
            return new StrictBinaryReader(stream);
        }

        private async Task<uint> ReadUInt32Async()
        {
            using (var reader = await LoadBinaryReaderAsync(_buffer, sizeof(UInt32)))
            {
                return reader.ReadUInt32();
            }
        }

        private class StrictBinaryReader : BinaryReader
        {
            public StrictBinaryReader(Stream input) : base(input)
            {
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (BaseStream.Position < BaseStream.Length)
                    {
                        throw new InvalidOperationException(Strings.BufferNotConsumed);
                    }
                }

                base.Dispose(disposing);
            }
        }
    }
}
