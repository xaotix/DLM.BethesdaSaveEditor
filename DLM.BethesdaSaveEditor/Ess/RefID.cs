using System;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public enum RefIDType
    {
        FormIdx = 0,
        Default = 1,
        Created = 2,
        Invalid = 3
    }

    public sealed class RefID : IEquatable<RefID>, IComparable<RefID>
    {
        public int RawData { get; }
        public int FormID { get; }
        public Plugin Plugin { get; }
        public RefIDType Type => (RefIDType)((RawData >> 22) & 0x03);
        public int ValuePart => RawData & 0x3FFFFF;
        public bool IsZero => ValuePart == 0;
        public bool IsValid => Type != RefIDType.Invalid && !IsZero;

        public RefID(int rawData, EssFile ess, bool pluginOverflow)
        {
            RawData = rawData & 0xFFFFFF;

            if (IsZero)
            {
                FormID = 0;
                Plugin = null;
            }
            else
            {
                switch (Type)
                {
                    case RefIDType.Default:
                        Plugin = (!pluginOverflow && ess.Plugins.FullPlugins.Count > 0)
                            ? ess.Plugins.FullPlugins[0]
                            : null;
                        FormID = ValuePart;
                        break;

                    case RefIDType.Created:
                        FormID = unchecked((int)0xFF000000) | ValuePart;
                        Plugin = PluginInfo.Created;
                        break;

                    case RefIDType.FormIdx:
                        int idx = ValuePart - 1;
                        if (ess.FormIDArray != null && idx >= 0 && idx < ess.FormIDArray.Length)
                        {
                            FormID = ess.FormIDArray[idx];
                            Plugin = ess.GetPluginFor(FormID);
                        }
                        else
                        {
                            FormID = -1;
                            Plugin = null;
                        }
                        break;

                    default:
                        FormID = 0;
                        Plugin = null;
                        break;
                }
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteRefIDRaw(RawData);
        }

        public int CalculateSize() => 3;

        public string ToHex()
        {
            string prefix = Plugin != null ? (Plugin.Name + ":") : string.Empty;
            string idStr = FormID != 0 ? FormID.ToString("X8") : RawData.ToString("X6");
            return prefix + idStr;
        }

        public override string ToString()
        {
            if (FormID == 0)
            {
                return string.Format("{0}:{1:X6}", Type, RawData);
            }
            return FormID.ToString("X8");
        }

        public bool Equals(RefID other)
        {
            return other != null && RawData == other.RawData;
        }

        public override bool Equals(object obj) => Equals(obj as RefID);

        public override int GetHashCode() => RawData;

        public int CompareTo(RefID other)
        {
            if (other == null) return 1;
            return ((uint)RawData).CompareTo((uint)other.RawData);
        }
    }
}

