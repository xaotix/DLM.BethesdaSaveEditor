using System;
using System.Text;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class Plugin : IComparable<Plugin>
    {
        public byte[] RawName { get; }
        public string Name { get; }
        public int Index { get; }
        public bool IsLightweight { get; }

        public string IndexDisplay => IsLightweight
            ? string.Format("FE{0:X3}", Index)
            : string.Format("{0:X2}", Index);

        public string TipoDisplay => IsLightweight ? "ESL (Leve)" : "ESP/ESM";

        public Plugin(byte[] rawName, int index, bool lightweight)
        {
            RawName = rawName ?? new byte[0];
            Name = Encoding.UTF8.GetString(RawName);
            Index = index;
            IsLightweight = lightweight;
        }

        public static Plugin ReadFull(BinaryReaderEx reader, int index)
        {
            return new Plugin(reader.ReadWStringRaw(), index, false);
        }

        public static Plugin ReadLite(BinaryReaderEx reader, int index)
        {
            return new Plugin(reader.ReadWStringRaw(), index, true);
        }

        public static Plugin CreatePseudoCreated()
        {
            return new Plugin(Encoding.UTF8.GetBytes("(Criado no Save)"), 0xFF, false);
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteWStringRaw(RawName);
        }

        public int CalculateSize() => 2 + RawName.Length;

        public int CompareTo(Plugin other)
        {
            if (other == null) return 1;
            if (IsLightweight != other.IsLightweight)
            {
                return IsLightweight ? 1 : -1;
            }
            return Index.CompareTo(other.Index);
        }

        public override string ToString() => string.Format("{0}: {1}", IndexDisplay, Name);
    }
}

