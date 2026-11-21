using System;
using System.IO;
using System.Text;

namespace DLM.BethesdaSaveEditor.Core
{
    public sealed class BinaryWriterEx : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly byte[] _tmp = new byte[8];

        public BinaryWriterEx(int capacity = 4096)
        {
            _stream = new MemoryStream(capacity);
        }

        public int Position => (int)_stream.Position;

        public void WriteByte(byte val)
        {
            _stream.WriteByte(val);
        }

        public void WriteInt16(short val)
        {
            _tmp[0] = (byte)(val & 0xFF);
            _tmp[1] = (byte)((val >> 8) & 0xFF);
            _stream.Write(_tmp, 0, 2);
        }

        public void WriteUInt16(ushort val)
        {
            WriteInt16((short)val);
        }

        public void WriteInt32(int val)
        {
            _tmp[0] = (byte)(val & 0xFF);
            _tmp[1] = (byte)((val >> 8) & 0xFF);
            _tmp[2] = (byte)((val >> 16) & 0xFF);
            _tmp[3] = (byte)((val >> 24) & 0xFF);
            _stream.Write(_tmp, 0, 4);
        }

        public void WriteUInt32(uint val)
        {
            WriteInt32((int)val);
        }

        public void WriteInt64(long val)
        {
            WriteUInt32((uint)(val & 0xFFFFFFFF));
            WriteUInt32((uint)((ulong)val >> 32));
        }

        public void WriteSingle(float val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            _stream.Write(bytes, 0, 4);
        }

        public void WriteBytes(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                _stream.Write(bytes, 0, bytes.Length);
            }
        }

        public void WriteWString(string texto)
        {
            byte[] raw = Encoding.UTF8.GetBytes(texto ?? string.Empty);
            WriteWStringRaw(raw);
        }

        public void WriteWStringRaw(byte[] raw)
        {
            byte[] data = raw ?? new byte[0];
            WriteUInt16((ushort)data.Length);
            WriteBytes(data);
        }

        public void WriteRefIDRaw(int data)
        {
            _tmp[0] = (byte)((data >> 16) & 0xFF);
            _tmp[1] = (byte)((data >> 8) & 0xFF);
            _tmp[2] = (byte)(data & 0xFF);
            _stream.Write(_tmp, 0, 3);
        }

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}

