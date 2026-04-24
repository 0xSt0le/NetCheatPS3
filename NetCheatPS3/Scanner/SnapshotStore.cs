using System;
using System.Collections.Generic;
using System.IO;

namespace NetCheatPS3.Scanner
{
    internal sealed class SnapshotStore : IDisposable
    {
        private const int Magic = 0x3153434E; // "NCS1" little-endian in file
        private const int Version = 1;

        private readonly FileStream _stream;
        private readonly BinaryWriter _writer;
        private readonly int _byteSize;
        private readonly long _countPosition;
        private long _count;
        private bool _completed;

        private SnapshotStore(string path, int typeIndex, int byteSize, bool littleEndian)
        {
            _byteSize = byteSize;
            _stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new BinaryWriter(_stream);

            _writer.Write(Magic);
            _writer.Write(Version);
            _writer.Write(typeIndex);
            _writer.Write(byteSize);
            _writer.Write(littleEndian ? 1 : 0);

            _countPosition = _stream.Position;
            _writer.Write((long)0);
        }

        public static SnapshotStore Create(string path, int typeIndex, int byteSize, bool littleEndian)
        {
            return new SnapshotStore(path, typeIndex, byteSize, littleEndian);
        }

        public void WriteRecord(ulong address, byte[] value)
        {
            if (value == null || value.Length < _byteSize)
                throw new ArgumentException("Snapshot value is null or smaller than byte size.");

            _writer.Write(address);
            _writer.Write(value, 0, _byteSize);
            _count++;
        }

        public void Complete()
        {
            if (_completed)
                return;

            _writer.Flush();

            long cur = _stream.Position;
            _stream.Position = _countPosition;
            _writer.Write(_count);
            _stream.Position = cur;

            _writer.Flush();
            _completed = true;
        }

        public void Dispose()
        {
            Complete();

            if (_writer != null)
                _writer.Close();

            if (_stream != null)
                _stream.Close();
        }

        public static SnapshotHeader ReadHeader(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int magic = br.ReadInt32();
                if (magic != Magic)
                    throw new InvalidDataException("Invalid snapshot file.");

                SnapshotHeader header = new SnapshotHeader();
                header.Version = br.ReadInt32();
                header.TypeIndex = br.ReadInt32();
                header.ByteSize = br.ReadInt32();
                header.LittleEndian = br.ReadInt32() != 0;
                header.Count = br.ReadInt64();

                if (header.Version != Version)
                    throw new InvalidDataException("Unsupported snapshot version.");

                if (header.ByteSize <= 0)
                    throw new InvalidDataException("Invalid snapshot byte size.");

                return header;
            }
        }

        public static IEnumerable<SnapshotRecord> ReadRecords(string path)
        {
            SnapshotHeader header;

            FileStream fs = null;
            BinaryReader br = null;

            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                br = new BinaryReader(fs);

                int magic = br.ReadInt32();
                if (magic != Magic)
                    throw new InvalidDataException("Invalid snapshot file.");

                header = new SnapshotHeader();
                header.Version = br.ReadInt32();
                header.TypeIndex = br.ReadInt32();
                header.ByteSize = br.ReadInt32();
                header.LittleEndian = br.ReadInt32() != 0;
                header.Count = br.ReadInt64();

                if (header.Version != Version)
                    throw new InvalidDataException("Unsupported snapshot version.");

                for (long i = 0; i < header.Count; i++)
                {
                    SnapshotRecord record = new SnapshotRecord();
                    record.Address = br.ReadUInt64();
                    record.Value = br.ReadBytes(header.ByteSize);

                    if (record.Value == null || record.Value.Length != header.ByteSize)
                        yield break;

                    yield return record;
                }
            }
            finally
            {
                if (br != null)
                    br.Close();

                if (fs != null)
                    fs.Close();
            }
        }
    }
}