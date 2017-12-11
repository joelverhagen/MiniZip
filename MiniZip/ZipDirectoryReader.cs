using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Based off of:
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipFile.cs
    /// https://github.com/icsharpcode/SharpZipLib/blob/4ad264b562579fc8d0c1f73812f69b78b49ebdee/src/ICSharpCode.SharpZipLib/Zip/ZipHelperStream.cs
    /// </summary>
    public class ZipDirectoryReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly byte[] _byteBuffer;

        public ZipDirectoryReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _byteBuffer = new byte[1];

            if (!_stream.CanSeek)
            {
                throw new ArgumentException("The stream is must be seekable.", nameof(stream));
            }
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }

        public async Task<ZipDirectory> ReadEntriesAsync()
        {
            var zip = new ZipDirectory();

            zip.OffsetAfterEndOfCentralDirectory = await LocateBlockWithSignatureAsync(
                ZipConstants.EndOfCentralDirectorySignature,
                _stream.Length,
                ZipConstants.EndOfCentralRecordBaseSize,
                0xffff);

            if (zip.OffsetAfterEndOfCentralDirectory < 0)
            {
                throw new ZipException("Cannot find central directory");
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
            zip.IsZip64 = false;
            long offsetOfCentralDirectory;
            ulong entriesInThisDisk;
            if (zip.NumberOfThisDisk == 0xffff
                || zip.DiskWithStartOfCentralDirectory == 0xffff
                || zip.EntriesInThisDisk == 0xffff
                || zip.EntriesForWholeCentralDirectory == 0xffff
                || zip.CentralDirectorySize == 0xffffffff
                || zip.OffsetOfCentralDirectory == 0xffffffff)
            {
                zip.IsZip64 = true;
                zip.Zip64 = new Zip64Directory();

                zip.Zip64.OffsetAfterEndOfCentralDirectoryLocator = await LocateBlockWithSignatureAsync(
                    ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature,
                    zip.OffsetAfterEndOfCentralDirectory,
                    0,
                    0x1000);

                if (zip.Zip64.OffsetAfterEndOfCentralDirectoryLocator < 0)
                {
                    throw new ZipException("Cannot find Zip64 locator");
                }

                zip.Zip64.DiskWithStartOfEndOfCentralDirectory = await ReadLEU32Async();
                zip.Zip64.EndOfCentralDirectoryOffset = await ReadLEU64Async();
                zip.Zip64.TotalNumberOfDisks = await ReadLEU32Async();

                _stream.Position = (long)zip.Zip64.EndOfCentralDirectoryOffset;

                if (await ReadLEU32Async() != ZipConstants.Zip64CentralFileHeaderSignature)
                {
                    throw new ZipException($"Invalid Zip64 Central directory signature at {zip.Zip64.EndOfCentralDirectoryOffset:X}");
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
                    throw new ZipException("The Zip64 metadata is not consistent with the non-Zip64 metadata.");
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
                if (await ReadLEU32Async() != ZipConstants.CentralHeaderSignature)
                {
                    throw new ZipException("Wrong central directory signature.");
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

                entry.Comment = new byte[entry.CommentSize];
                await ReadFullyAsync(entry.Comment);

                zip.Entries.Add(entry);
            }

            return zip;
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
            var count = buffer.Length;
            var offset = 0;

            while (count > 0)
            {
                var read = await _stream.ReadAsync(buffer, offset, count);
                if (read <= 0)
                {
                    throw new EndOfStreamException();
                }

                offset += read;
                count -= read;
            }
        }

        private async Task<byte> ReadU8Async()
        {
            if (await _stream.ReadAsync(_byteBuffer, 0, 1) == 0)
            {
                throw new EndOfStreamException();
            }

            return _byteBuffer[0];
        }

        private async Task<ushort> ReadLEU16Async()
        {
            int byteA = await ReadU8Async();
            int byteB = await ReadU8Async();

            return unchecked((ushort)((ushort)byteA | (ushort)(byteB << 8)));
        }

        private async Task<uint> ReadLEU32Async()
        {
            return (uint)(await ReadLEU16Async() | (await ReadLEU16Async() << 16));
        }

        private async Task<ulong> ReadLEU64Async()
        {
            return await ReadLEU32Async() | ((ulong)(await ReadLEU32Async()) << 32);
        }
    }
}
