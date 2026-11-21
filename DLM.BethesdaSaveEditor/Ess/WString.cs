using System;
using System.Text;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class WString
    {
        private byte[] _raw;
        private string _text;

        public WString(byte[] raw)
        {
            _raw = raw ?? new byte[0];
            _text = Encoding.UTF8.GetString(_raw);
        }

        public WString(string text)
        {
            _text = text ?? string.Empty;
            _raw = Encoding.UTF8.GetBytes(_text);
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? string.Empty;
                _raw = Encoding.UTF8.GetBytes(_text);
            }
        }

        public byte[] RawBytes => _raw;

        public static WString Read(BinaryReaderEx reader)
        {
            return new WString(reader.ReadWStringRaw());
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteWStringRaw(_raw);
        }

        public int CalculateSize() => 2 + _raw.Length;

        public override string ToString() => _text;

        public override bool Equals(object obj)
        {
            WString other = obj as WString;
            return other != null && string.Equals(_text, other._text, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_text ?? string.Empty);
        }
    }
}

