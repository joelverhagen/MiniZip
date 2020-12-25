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
        private static readonly ILookup<string, string> EmptyProperties = Enumerable.Empty<string>().ToLookup(x => x, x => x);

        private static readonly int BufferSize = new[]
        {
            ZipConstants.EndOfCentralDirectorySizeWithoutSignature,
            ZipConstants.Zip64EndOfCentralDirectoryLocatorSizeWithoutSignature,
            ZipConstants.Zip64EndOfCentralDirectorySizeWithoutSignature,
            ZipConstants.CentralDirectoryEntryHeaderSizeWithoutSignature,
            sizeof(uint),
            4096,
        }.Max();

        private Stream _stream;
        private readonly byte[] _buffer;
        private readonly long _minimumPosition;
        private readonly bool _leaveOpen;
        private int _disposed;
        private readonly ILookup<string, string> _properties;

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
        public ZipDirectoryReader(Stream stream, bool leaveOpen) : this(stream, leaveOpen, EmptyProperties)
        {
        }

        /// <summary>
        /// Initializes a ZIP directory reader, which reads the provided <see cref="Stream"/>. Whether or not the
        /// provided stream is disposed when this instance is disposed is controlled by <paramref name="leaveOpen"/>.
        /// </summary>
        /// <param name="stream">The stream containing the ZIP archive.</param>
        /// <param name="leaveOpen">Whether or not to leave the stream open when this instance is disposed.</param>
        /// <param name="properties">A property bag related to the ZIP directory. For HTTP-based ZIP directories, this is the HTTP headers.</param>
        public ZipDirectoryReader(Stream stream, bool leaveOpen, ILookup<string, string> properties)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _buffer = new byte[BufferSize];
            _minimumPosition = (stream as VirtualOffsetStream)?.VirtualOffset ?? 0;
            _leaveOpen = leaveOpen;
            _disposed = 0;
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));

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
        /// The stream containing the ZIP archive that is read by this instance.
        /// </summary>
        public Stream Stream => _stream;

        /// <summary>
        /// A property bag related to the ZIP directory. For HTTP-based ZIP directories, this is the HTTP headers.
        /// </summary>
        public ILookup<string, string> Properties => _properties;

        /// <summary>
        /// Reads a specific local file header given a central directory header for a file.
        /// </summary>
        /// <param name="directory">The ZIP directory instance containing the provided <paramref name="entry"/>.</param>
        /// <param name="entry">The central directory header to read the local file header for.</param>
        /// <returns>The local file header.</returns>
        public async Task<LocalFileHeader> ReadLocalFileHeaderAsync(ZipDirectory directory, CentralDirectoryHeader entry)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var entries = directory.Entries ?? new List<CentralDirectoryHeader>();
            if (!entries.Contains(entry))
            {
                throw new ArgumentException(Strings.HeaderDoesNotMatchDirectory, nameof(entry));
            }

            if ((entry.Flags & (ushort)ZipEntryFlags.Encrypted) != 0)
            {
                throw new MiniZipException(Strings.EncryptedFilesNotSupported);
            }

            var zip64 = entry.Zip64DataFields.FirstOrDefault();
            var localHeaderOffset = zip64?.LocalHeaderOffset ?? entry.LocalHeaderOffset;
            _stream.Position = (long)localHeaderOffset;

            if (await ReadUInt32Async() != ZipConstants.LocalFileHeaderSignature)
            {
                throw new MiniZipException(Strings.InvalidLocalFileHeaderSignature);
            }

            var localEntry = new LocalFileHeader();

            using (var buffer = await LoadBinaryReaderAsync(_buffer, ZipConstants.LocalFileHeaderSizeWithoutSignature))
            {
                localEntry.VersionToExtract = buffer.ReadUInt16();
                localEntry.Flags = buffer.ReadUInt16();
                localEntry.CompressionMethod = buffer.ReadUInt16();
                localEntry.LastModifiedTime = buffer.ReadUInt16();
                localEntry.LastModifiedDate = buffer.ReadUInt16();
                localEntry.Crc32 = buffer.ReadUInt32();
                localEntry.CompressedSize = buffer.ReadUInt32();
                localEntry.UncompressedSize = buffer.ReadUInt32();
                localEntry.NameSize = buffer.ReadUInt16();
                localEntry.ExtraFieldSize = buffer.ReadUInt16();
            }

            localEntry.Name = new byte[localEntry.NameSize];
            await ReadFullyAsync(localEntry.Name);

            localEntry.ExtraField = new byte[localEntry.ExtraFieldSize];
            await ReadFullyAsync(localEntry.ExtraField);

            localEntry.DataFields = ReadDataFields(localEntry.ExtraField);
            localEntry.Zip64DataFields = ReadZip64DataFields(localEntry);

            // Try to read the data descriptor.
            if ((localEntry.Flags & (ushort)ZipEntryFlags.DataDescriptor) != 0)
            {
                localEntry.DataDescriptor = await ReadDataDescriptor(directory, entries, entry, zip64, localEntry);
            }

            return localEntry;
        }

        private async Task<DataDescriptor> ReadDataDescriptor(
            ZipDirectory directory,
            List<CentralDirectoryHeader> entries,
            CentralDirectoryHeader entry,
            Zip64DataField zip64,
            LocalFileHeader localEntry)
        {
            var compressedSize = zip64?.CompressedSize ?? entry.CompressedSize;
            var uncompressedSize = zip64?.UncompressedSize ?? entry.UncompressedSize;

            var dataDescriptorPosition = entry.LocalHeaderOffset
                + ZipConstants.LocalFileHeaderSize
                + localEntry.NameSize
                + localEntry.ExtraFieldSize
                + compressedSize;

            _stream.Position = (long)dataDescriptorPosition;

            var dataDescriptor = new DataDescriptor();

            using (var buffer = await LoadBinaryReaderAsync(_buffer, ZipConstants.DataDescriptorSize))
            {
                var fieldA = buffer.ReadUInt32();
                var fieldB = buffer.ReadUInt32();
                var fieldC = buffer.ReadUInt32();
                var fieldD = buffer.ReadUInt32();

                // Check the first field to see if is the signature. This is the most reliable check but can yield
                // false negatives. This is because it's possible for the CRC-32 to be the same as the optional
                // data descriptor signature. There is no possibility for false positive. That is, if the first
                // byte does not match signature, then the first byte is definitely the CRC-32.
                var firstFieldImpliesNoSignature = fieldA != ZipConstants.DataDescriptorSignature;

                // 1. Use the known field values from the central directory, given the first field matches the signature.
                var valuesImplySignature = !firstFieldImpliesNoSignature
                    && fieldB == entry.Crc32
                    && fieldC == compressedSize
                    && fieldD == uncompressedSize;
                if (valuesImplySignature)
                {
                    return CreateDataDescriptor(fieldB, fieldC, fieldD, hasSignature: true);
                }

                // 2. Use the known field values from the central directory.
                var valuesImplyNoSignature = fieldA == entry.Crc32
                    && fieldB == compressedSize
                    && fieldC == uncompressedSize;
                if (valuesImplyNoSignature)
                {
                    return CreateDataDescriptor(fieldA, fieldB, fieldC, hasSignature: false);
                }

                // 3. Use just the signature.
                if (!firstFieldImpliesNoSignature)
                {
                    return CreateDataDescriptor(fieldB, fieldC, fieldD, hasSignature: true);
                }

                return CreateDataDescriptor(fieldA, fieldB, fieldC, hasSignature: false);
            }
        }

        private static DataDescriptor CreateDataDescriptor(uint crc32, uint compressedSize, uint uncompressedSize, bool hasSignature)
        {
            return new DataDescriptor
            {
                HasSignature = hasSignature,
                Crc32 = crc32,
                CompressedSize = compressedSize,
                UncompressedSize = uncompressedSize,
            };
        }

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

            var lazyBuffer = new Lazy<byte[]>(() => new byte[0xffff]);

            zip.OffsetAfterEndOfCentralDirectory = await LocateBlockWithSignatureAsync(
                ZipConstants.EndOfCentralDirectorySignature,
                _stream.Length,
                ZipConstants.EndOfCentralDirectorySize,
                0xffff,
                lazyBuffer);

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
            ulong numberOfThisDisk;
            ulong diskWithStartOfCentralDirectory;
            ulong entriesInThisDisk;
            ulong entriesForWholeCentralDirectory;
            long offsetOfCentralDirectory;
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
                    zip.OffsetAfterEndOfCentralDirectory - sizeof(uint),
                    ZipConstants.Zip64EndOfCentralDirectoryLocatorSize,
                    0x1000,
                    lazyBuffer);

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

                Seek((long)zip.Zip64.EndOfCentralDirectoryOffset);

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

                numberOfThisDisk = zip.Zip64.NumberOfThisDisk;
                diskWithStartOfCentralDirectory = zip.Zip64.DiskWithStartOfCentralDirectory;
                entriesInThisDisk = zip.Zip64.EntriesInThisDisk;
                entriesForWholeCentralDirectory = zip.Zip64.EntriesForWholeCentralDirectory;
                offsetOfCentralDirectory = (long)zip.Zip64.OffsetOfCentralDirectory;

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
                numberOfThisDisk = zip.NumberOfThisDisk;
                diskWithStartOfCentralDirectory = zip.DiskWithStartOfCentralDirectory;
                entriesInThisDisk = zip.EntriesInThisDisk;
                entriesForWholeCentralDirectory = zip.EntriesForWholeCentralDirectory;
                offsetOfCentralDirectory = zip.OffsetOfCentralDirectory;
            }

            if (numberOfThisDisk != 0
                || diskWithStartOfCentralDirectory != 0
                || entriesInThisDisk != entriesForWholeCentralDirectory)
            {
                throw new MiniZipException(Strings.ArchivesSpanningMultipleDisksNotSupported);
            }

            Seek(offsetOfCentralDirectory);

            zip.Entries = new List<CentralDirectoryHeader>();
            for (ulong i = 0; i < entriesInThisDisk; i++)
            {
                zip.Entries.Add(await ReadEntryAsync());
            }

            return zip;
        }

        private async Task<CentralDirectoryHeader> ReadEntryAsync()
        {
            if (await ReadUInt32Async() != ZipConstants.CentralDirectoryEntryHeaderSignature)
            {
                throw new MiniZipException(Strings.InvalidCentralDirectorySignature);
            }

            var entry = new CentralDirectoryHeader();

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

        private List<Zip64CentralDirectoryDataField> ReadZip64DataFields(CentralDirectoryHeader entry)
        {
            return ReadZip64DataFields(entry, ZipConstants.MaximumZip64CentralDirectoryDataFieldSize);
        }

        private List<Zip64LocalFileDataField> ReadZip64DataFields(LocalFileHeader entry)
        {
            // The Zip64 data field in the local file header is a subset of the information in the central directory
            // header, so use the central directory flow and map the input/output.
            var centralDirectoryHeader = new CentralDirectoryHeader
            {
                DataFields = entry.DataFields,
                UncompressedSize = entry.UncompressedSize,
                CompressedSize = entry.CompressedSize,
            };

            var fields = ReadZip64DataFields(centralDirectoryHeader, ZipConstants.MaximumZip64LocalFileDataFieldSize);

            return fields
                .Select(x => new Zip64LocalFileDataField
                {
                    CompressedSize = x.CompressedSize,
                    UncompressedSize = x.UncompressedSize,
                })
                .ToList();
        }

        private List<Zip64CentralDirectoryDataField> ReadZip64DataFields(CentralDirectoryHeader entry, ushort maximumDataSize)
        {
            var zip64DataFields = new List<Zip64CentralDirectoryDataField>();
            foreach (var dataField in entry.DataFields)
            {
                if (dataField.HeaderId != ZipConstants.Zip64DataFieldHeaderId)
                {
                    continue;
                }

                if (dataField.DataSize > maximumDataSize)
                {
                    throw new MiniZipException(Strings.InvalidZip64ExtendedInformationLength);
                }

                using (var reader = GetBinaryReader(dataField.Data))
                {
                    var field = new Zip64CentralDirectoryDataField();

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
            int maximumVariableData,
            Lazy<byte[]> lazyBuffer)
        {
            var pos = endLocation - minimumBlockSize;
            if (pos < 0)
            {
                return -1;
            }

            var giveUpMarker = Math.Max(Math.Max(pos - maximumVariableData, 0), _minimumPosition);
            if (pos < giveUpMarker)
            {
                return -1;
            }

            Seek(pos);
            var candidate = await ReadUInt32Async();
            if (signature == candidate)
            {
                return _stream.Position;
            }

            while (true)
            {
                var streamPosition = Math.Max(giveUpMarker, pos - lazyBuffer.Value.Length);
                var readAmount = (int)(pos - streamPosition);
                if (readAmount < 1)
                {
                    return -1;
                }

                Seek(streamPosition);
                await _stream.ReadExactlyAsync(lazyBuffer.Value, 0, readAmount);

                for (var bufferPos = readAmount - 1; bufferPos >= 0; bufferPos--)
                {
                    pos--;
                    candidate <<= sizeof(byte) * 8;
                    candidate |= lazyBuffer.Value[bufferPos];

                    if (signature == candidate)
                    {
                        Seek(pos + sizeof(uint));
                        return _stream.Position;
                    }
                }
            }
        }

        private void Seek(long pos)
        {
            if (pos < _minimumPosition)
            {
                throw new InvalidOperationException(
                    $"Unable seek to position {pos} which is before the minimum position restrition " +
                    $"{_minimumPosition}.");
            }

            _stream.Seek(pos, SeekOrigin.Begin);
        }

        private async Task ReadFullyAsync(byte[] buffer)
        {
            await _stream.ReadExactlyAsync(buffer, 0, buffer.Length);
        }

        private async Task<BinaryReader> LoadBinaryReaderAsync(byte[] buffer, int count)
        {
            await _stream.ReadExactlyAsync(buffer, 0, count);
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
            using (var reader = await LoadBinaryReaderAsync(_buffer, sizeof(uint)))
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
