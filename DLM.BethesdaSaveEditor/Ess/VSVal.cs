using System;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class VSVal
    {
        private readonly byte[] _data;

        public VSVal(int value)
        {
            if (value <= 0x3F)
            {
                _data = new byte[] { (byte)(value << 2) };
            }
            else if (value <= 0x3FFF)
            {
                int v = (value << 2) | 0x01;
                _data = new byte[] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF) };
            }
            else if (value <= 0x3FFFFFFF)
            {
                int v = (value << 2) | 0x02;
                _data = new byte[]
                {
                    (byte)(v & 0xFF),
                    (byte)((v >> 8) & 0xFF),
                    (byte)((v >> 16) & 0xFF)
                };
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Valor VSVal excede o limite suportado.");
            }
        }

        public VSVal(BinaryReaderEx reader)
        {
            byte first = reader.ReadByte();
            int sizeType = first & 0x03;
            switch (sizeType)
            {
                case 0:
                    _data = new byte[] { first };
                    break;
                case 1:
                    _data = new byte[] { first, reader.ReadByte() };
                    break;
                default:
                    _data = new byte[] { first, reader.ReadByte(), reader.ReadByte() };
                    break;
            }
        }

        public int Value
        {
            get
            {
                int sizeType = _data[0] & 0x03;
                switch (sizeType)
                {
                    case 0:
                        return _data[0] >> 2;
                    case 1:
                        return (_data[0] | (_data[1] << 8)) >> 2;
                    default:
                        return (_data[0] | (_data[1] << 8) | (_data[2] << 16)) >> 2;
                }
            }
        }

        public int CalculateSize() => _data.Length;

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteBytes(_data);
        }

        public override string ToString() => Value.ToString();
    }
}

