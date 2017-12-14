using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Based off of:
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipFile.cs
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipHelperStream.cs
    /// </summary>
    public class ZipDirectoryReader : IZipDirectoryReader
    {
        private Stream _stream;
        private readonly byte[] _byteBuffer;
        private readonly bool _leaveOpen;
        private int _disposed;

        public ZipDirectoryReader(Stream stream) : this(stream, leaveOpen: false)
        {
        }

        public ZipDirectoryReader(Stream stream, bool leaveOpen)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _byteBuffer = new byte[1];
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

        public bool IsDisposed => _disposed != 0;

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
                ZipConstants.EndOfCentralRecordBaseSize,
                0xffff);

            if (zip.OffsetAfterEndOfCentralDirectory < 0)
            {
                throw new MiniZipException(Strings.CannotFindCentralDirectory);
            }

            zip.NumberOfThisDisk = await ReadLEU16Async();
            zip.DiskWithStartOfCentralDirectory = await ReadLEU16Async();
            zip.EntriesInThisDisk = await ReadLEU16Async();
            zip.EntriesForWholeCentralDirectory = await ReadLEU16Async();
            zip.CentralDirectorySize = await ReadLEU32Async();
            zip.OffsetOfCentralDirectory = await ReadLEU32Async();
            zip.CommentSize = await ReadLEU16Async();
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

                zip.Zip64.DiskWithStartOfEndOfCentralDirectory = await ReadLEU32Async();
                zip.Zip64.EndOfCentralDirectoryOffset = await ReadLEU64Async();
                zip.Zip64.TotalNumberOfDisks = await ReadLEU32Async();

                _stream.Position = (long)zip.Zip64.EndOfCentralDirectoryOffset;

                if (await ReadLEU32Async() != ZipConstants.Zip64CentralFileHeaderSignature)
                {
                    throw new MiniZipException(Strings.InvalidZip64CentralDirectorySignature);
                }

                zip.Zip64.SizeOfCentralDirectoryRecord = await ReadLEU64Async();
                zip.Zip64.VersionMadeBy = await ReadLEU16Async(); 
                zip.Zip64.VersionToExtract = await ReadLEU16Async();
                zip.Zip64.NumberOfThisDisk = await ReadLEU32Async();
                zip.Zip64.DiskWithStartOfCentralDirectory =  await ReadLEU32Async();
                zip.Zip64.EntriesInThisDisk = await ReadLEU64Async();
                zip.Zip64.EntriesForWholeCentralDirectory = await ReadLEU64Async();
                zip.Zip64.CentralDirectorySize = await ReadLEU64Async();
                zip.Zip64.OffsetOfCentralDirectory = await ReadLEU64Async();

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
            if (await ReadLEU32Async() != ZipConstants.CentralHeaderSignature)
            {
                throw new MiniZipException(Strings.InvalidCentralDirectorySignature);
            }

            var entry = new ZipEntry();

            entry.VersionMadeBy = await ReadLEU16Async();
            entry.VersionToExtract = await ReadLEU16Async();
            entry.Flags = await ReadLEU16Async();
            entry.CompressionMethod = await ReadLEU16Async();
            entry.LastModifiedTime = await ReadLEU16Async();
            entry.LastModifiedDate = await ReadLEU16Async();
            entry.Crc32 = await ReadLEU32Async();
            entry.CompressedSize = await ReadLEU32Async();
            entry.UncompressedSize = await ReadLEU32Async();
            entry.NameSize = await ReadLEU16Async();
            entry.ExtraFieldSize = await ReadLEU16Async();
            entry.CommentSize = await ReadLEU16Async();
            entry.DiskNumberStart = await ReadLEU16Async();
            entry.InternalAttributes = await ReadLEU16Async();
            entry.ExternalAttributes = await ReadLEU32Async();
            entry.LocalHeaderOffset = await ReadLEU32Async();

            entry.Name = new byte[entry.NameSize];
            await ReadFullyAsync(entry.Name);

            entry.ExtraField = new byte[entry.ExtraFieldSize];
            await ReadFullyAsync(entry.ExtraField);

            entry.DataFields = await ReadDataFieldsAsync(entry.ExtraField);
            entry.Zip64DataFields = await ReadZip64DataFields(entry);

            entry.Comment = new byte[entry.CommentSize];
            await ReadFullyAsync(entry.Comment);

            return entry;
        }

        private async Task<List<ZipDataField>> ReadDataFieldsAsync(byte[] extraField)
        {
            var dataFields = new List<ZipDataField>();
            using (var extraStream = new MemoryStream(extraField))
            {
                while (extraStream.Position < extraStream.Length)
                {
                    var dataField = new ZipDataField();

                    dataField.HeaderId = await extraStream.ReadLEU16Async(_byteBuffer);
                    dataField.DataSize = await extraStream.ReadLEU16Async(_byteBuffer);

                    dataField.Data = new byte[dataField.DataSize];
                    await extraStream.ReadFullyAsync(dataField.Data);

                    dataFields.Add(dataField);
                }
            }

            return dataFields;
        }

        private async Task<List<Zip64DataField>> ReadZip64DataFields(ZipEntry entry)
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

                using (var stream = new MemoryStream(dataField.Data))
                {
                    var field = new Zip64DataField();

                    if (entry.UncompressedSize == 0xffffffff)
                    {
                        field.UncompressedSize = await stream.ReadLEU64Async(_byteBuffer);
                    }

                    if (entry.CompressedSize == 0xffffffff)
                    {
                        field.CompressedSize = await stream.ReadLEU64Async(_byteBuffer);
                    }

                    if (entry.LocalHeaderOffset == 0xffffffff)
                    {
                        field.LocalHeaderOffset = await stream.ReadLEU64Async(_byteBuffer);
                    }

                    if (entry.DiskNumberStart == 0xffff)
                    {
                        field.DiskNumberStart = await stream.ReadLEU32Async(_byteBuffer);
                    }

                    if (stream.Position < stream.Length)
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
            while (await ReadLEU32Async() != signature);

            return _stream.Position;
        }
        
        private async Task ReadFullyAsync(byte[] buffer)
        {
            await _stream.ReadFullyAsync(buffer);
        }

        private async Task<ushort> ReadLEU16Async()
        {
            return await _stream.ReadLEU16Async(_byteBuffer);
        }

        private async Task<uint> ReadLEU32Async()
        {
            return await _stream.ReadLEU32Async(_byteBuffer);
        }

        private async Task<ulong> ReadLEU64Async()
        {
            return await _stream.ReadLEU64Async(_byteBuffer);
        }
    }
}
