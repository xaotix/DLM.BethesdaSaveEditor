using System;
using System.IO;
using System.Text;

namespace DLM.BethesdaSaveEditor.Core
{
    public sealed class BinaryReaderEx
    {
        private readonly byte[] _buffer;
        private readonly int _offset;
        private readonly int _length;
        private int _pos;

        public BinaryReaderEx(byte[] buffer, int offset = 0, int length = -1)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _offset = offset;
            _length = length < 0 ? (buffer.Length - offset) : length;
            _pos = 0;
        }

        public int Position
        {
            get => _pos;
            set
            {
                if (value < 0 || value > _length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Posicao fora dos limites do buffer.");
                }
                _pos = value;
            }
        }

        public int Length => _length;
        public int Remaining => _length - _pos;

        private void Ensure(int count)
        {
            if (count < 0 || _pos + count > _length)
            {
                throw new EndOfStreamException(
                    string.Format("Fim inesperado do arquivo (pos={0}, req={1}, len={2}).", _pos, count, _length));
            }
        }

        public byte ReadByte()
        {
            Ensure(1);
            return _buffer[_offset + _pos++];
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public short ReadInt16()
        {
            Ensure(2);
            int p = _offset + _pos;
            _pos += 2;
            return (short)(_buffer[p] | (_buffer[p + 1] << 8));
        }

        public ushort ReadUInt16()
        {
            Ensure(2);
            int p = _offset + _pos;
            _pos += 2;
            return (ushort)(_buffer[p] | (_buffer[p + 1] << 8));
        }

        public int ReadInt32()
        {
            Ensure(4);
            int p = _offset + _pos;
            _pos += 4;
            return _buffer[p]
                | (_buffer[p + 1] << 8)
                | (_buffer[p + 2] << 16)
                | (_buffer[p + 3] << 24);
        }

        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        public long ReadInt64()
        {
            uint lo = ReadUInt32();
            uint hi = ReadUInt32();
            return (long)(((ulong)hi << 32) | lo);
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        public float ReadSingle()
        {
            Ensure(4);
            float val = BitConverter.ToSingle(_buffer, _offset + _pos);
            _pos += 4;
            return val;
        }

        public byte[] ReadBytes(int count)
        {
            Ensure(count);
            byte[] dst = new byte[count];
            Buffer.BlockCopy(_buffer, _offset + _pos, dst, 0, count);
            _pos += count;
            return dst;
        }

        public string ReadFixedAscii(int count)
        {
            byte[] bytes = ReadBytes(count);
            return Encoding.ASCII.GetString(bytes);
        }

        public byte[] ReadWStringRaw()
        {
            ushort len = ReadUInt16();
            return ReadBytes(len);
        }

        public string ReadWString()
        {
            byte[] raw = ReadWStringRaw();
            return Encoding.UTF8.GetString(raw);
        }

        public byte[] ReadLStringRaw()
        {
            int len = ReadInt32();
            if (len < 0)
            {
                throw new InvalidDataException("Comprimento invalido de LString: " + len);
            }
            return ReadBytes(len);
        }

        public BinaryReaderEx Slice(int length)
        {
            Ensure(length);
            BinaryReaderEx sub = new BinaryReaderEx(_buffer, _offset + _pos, length);
            _pos += length;
            return sub;
        }

        public int ReadRefIDRaw()
        {
            Ensure(3);
            int p = _offset + _pos;
            _pos += 3;
            return (_buffer[p] << 16) | (_buffer[p + 1] << 8) | _buffer[p + 2];
        }
    }
}

