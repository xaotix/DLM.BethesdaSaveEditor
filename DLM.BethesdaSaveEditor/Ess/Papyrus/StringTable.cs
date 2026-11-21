using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess.Papyrus
{
    public sealed class StringTable
    {
        private readonly List<TString> _strings = new List<TString>();

        public bool Is32Bit { get; }
        public bool HasStringTableBug { get; }
        public int Count => _strings.Count;
        public IReadOnlyList<TString> Strings => _strings;

        public StringTable(BinaryReaderEx reader, EssFile ess)
        {
            Is32Bit = ess.IsStr32;
            int strCount;

            if (Is32Bit)
            {
                strCount = reader.ReadInt32();
                HasStringTableBug = false;
            }
            else
            {
                strCount = reader.ReadUInt16();
                if (strCount == 0xFFFF)
                {
                    strCount = reader.ReadInt32();
                }

                if ((ess.Header.Game.EhFallout4() && strCount < 7000) || (ess.Header.Game.EhSkyrim() && strCount < 20000))
                {
                    strCount |= 0x10000;
                    HasStringTableBug = true;
                }
                else
                {
                    HasStringTableBug = false;
                }
            }

            _strings.Capacity = strCount;
            for (int i = 0; i < strCount; i++)
            {
                WString wstr = WString.Read(reader);
                _strings.Add(new TString(wstr, i, Is32Bit));
            }
        }

        public TString ReadReference(BinaryReaderEx reader)
        {
            int index;
            if (Is32Bit)
            {
                index = reader.ReadInt32();
            }
            else
            {
                index = reader.ReadUInt16();
                if (index == 0xFFFF && !HasStringTableBug)
                {
                    index = reader.ReadInt32();
                }
            }

            if (index < 0 || index >= _strings.Count)
            {
                throw new InvalidDataException(string.Format("Invalid TString index: {0}/{1}", index, _strings.Count));
            }
            return _strings[index];
        }

        public TString AddString(string text)
        {
            string val = text ?? string.Empty;
            TString existing = _strings.FirstOrDefault(s => string.Equals(s.Text, val, StringComparison.Ordinal));
            if (existing != null)
            {
                return existing;
            }

            TString created = new TString(new WString(val), _strings.Count, Is32Bit);
            _strings.Add(created);
            return created;
        }

        public void EditString(int index, string newText)
        {
            if (index < 0 || index >= _strings.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _strings[index].Value.Text = newText ?? string.Empty;
        }

        public void Write(BinaryWriterEx writer)
        {
            if (HasStringTableBug)
            {
                throw new InvalidOperationException("String-Table-Bug detected; cannot safely write.");
            }

            if (Is32Bit)
            {
                writer.WriteInt32(_strings.Count);
            }
            else
            {
                if (_strings.Count > 0xFFF0)
                {
                    writer.WriteUInt16(0xFFFF);
                    writer.WriteInt32(_strings.Count);
                }
                else
                {
                    writer.WriteUInt16((ushort)_strings.Count);
                }
            }

            foreach (TString s in _strings)
            {
                s.WriteFull(writer);
            }
        }

        public int CalculateSize()
        {
            int sum;
            if (Is32Bit) sum = 4;
            else if (_strings.Count > 0xFFF0 && !HasStringTableBug) sum = 6;
            else sum = 2;

            sum += _strings.Sum(s => s.CalculateFullSize());
            return sum;
        }
    }
}
