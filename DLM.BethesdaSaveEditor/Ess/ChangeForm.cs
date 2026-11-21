using System;
using System.Collections.Generic;
using System.Text;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class ChangeForm
    {
        public RefID RefID { get; }
        public uint ChangeFlags { get; set; }
        public byte RawTypeField { get; private set; }
        public byte Version { get; }
        public int Length1 { get; private set; }
        public int Length2 { get; private set; }
        public bool IsCompressed => Length2 > 0;
        public byte[] RawData { get; private set; }
        public string TipoNome { get; }
        public string TipoCodigo { get; }

        public string FormIDHex => RefID.ToHex();
        public string PluginNome => RefID.Plugin != null ? RefID.Plugin.Name : "(None)";
        public string FlagsHex => "0x" + ChangeFlags.ToString("X8");
        public int TamanhoDescompactado => IsCompressed ? Length2 : Length1;
        public string CompressaoTexto => IsCompressed ? "ZLIB" : "None";

        public ChangeForm(BinaryReaderEx reader, EssFile ess)
        {
            RefID = ess.ReadRefID(reader);
            ChangeFlags = reader.ReadUInt32();
            RawTypeField = reader.ReadByte();
            Version = reader.ReadByte();

            int typeCode = RawTypeField & 0x3F;
            TipoCodigo = ObterCodigoTipo(ess.Header.Game, typeCode);
            TipoNome = ObterDescricaoTipo(TipoCodigo);

            int lenType = (RawTypeField >> 6) & 0x03;
            switch (lenType)
            {
                case 0:
                    Length1 = reader.ReadByte();
                    Length2 = reader.ReadByte();
                    break;
                case 1:
                    Length1 = reader.ReadUInt16();
                    Length2 = reader.ReadUInt16();
                    break;
                case 2:
                    Length1 = reader.ReadInt32();
                    Length2 = reader.ReadInt32();
                    break;
                default:
                    throw new InvalidOperationException("Invalid ChangeForm length type: " + lenType);
            }

            RawData = reader.ReadBytes(Length1);
        }

        public byte[] GetDecompressedBody()
        {
            if (!IsCompressed) return RawData;
            return CompressionHelper.InflateZlib(RawData, Length2);
        }

        public void UpdateBodyData(byte[] uncompressedBody)
        {
            if (IsCompressed)
            {
                byte[] compressed = CompressionHelper.DeflateZlib(uncompressedBody);
                RawData = compressed;
                Length1 = compressed.Length;
                Length2 = uncompressedBody.Length;
            }
            else
            {
                RawData = uncompressedBody;
                Length1 = uncompressedBody.Length;
                Length2 = 0;
            }

            // Garante que o tipo de tamanho (8/16/32 bit) comporte os novos comprimentos
            int maxLen = Math.Max(Length1, Length2);
            int rawType = RawTypeField & 0x3F;
            if (maxLen <= 0xFF) RawTypeField = (byte)rawType;
            else if (maxLen <= 0xFFFF) RawTypeField = (byte)(rawType | 0x40);
            else RawTypeField = (byte)(rawType | 0x80);
        }

        /// <summary>
        /// Purifica um ChangeForm FLST removendo referencias nulas (0x000000).
        /// </summary>
        public int PurifyFormList(EssFile ess)
        {
            if (TipoCodigo != "FLST") return 0;
            bool hasFormFlags = (ChangeFlags & (1u << 1)) != 0;
            bool hasAddedForms = (ChangeFlags & (1u << 31)) != 0;
            if (!hasAddedForms) return 0;

            byte[] body = GetDecompressedBody();
            if (body == null || body.Length < 4) return 0;

            BinaryReaderEx r = new BinaryReaderEx(body);
            byte[] flagsBytes = hasFormFlags ? r.ReadBytes(6) : null;
            if (r.Remaining < 4) return 0;

            int count = r.ReadInt32();
            if (count <= 0 || count > 0x3FFF || r.Remaining < count * 3) return 0;

            List<RefID> valid = new List<RefID>(count);
            int removed = 0;
            for (int i = 0; i < count; i++)
            {
                RefID rf = ess.ReadRefID(r);
                if (rf.IsZero) removed++;
                else valid.Add(rf);
            }

            if (removed == 0) return 0;

            byte[] tail = r.ReadBytes(r.Remaining);
            using (BinaryWriterEx w = new BinaryWriterEx(body.Length))
            {
                if (flagsBytes != null) w.WriteBytes(flagsBytes);
                w.WriteInt32(valid.Count);
                foreach (RefID rf in valid) rf.Write(w);
                w.WriteBytes(tail);
                UpdateBodyData(w.ToArray());
            }

            return removed;
        }

        public string GerarHexDump(int maxBytes = 4096)
        {
            byte[] data;
            try { data = GetDecompressedBody(); }
            catch { data = RawData; }

            if (data == null || data.Length == 0) return "(No data)";

            int len = Math.Min(data.Length, maxBytes);
            StringBuilder sb = new StringBuilder(len * 4);
            for (int i = 0; i < len; i += 16)
            {
                sb.AppendFormat("{0:X8}  ", i);
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < len) sb.AppendFormat("{0:X2} ", data[i + j]);
                    else sb.Append("   ");
                    if (j == 7) sb.Append(' ');
                }
                sb.Append(" |");
                for (int j = 0; j < 16 && i + j < len; j++)
                {
                    byte b = data[i + j];
                    sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }
                sb.AppendLine("|");
            }

            if (data.Length > maxBytes)
            {
                sb.AppendFormat("... ({0:N0} total bytes, showing first {1:N0} bytes)", data.Length, maxBytes);
            }
            return sb.ToString();
        }

        public void Write(BinaryWriterEx writer)
        {
            RefID.Write(writer);
            writer.WriteUInt32(ChangeFlags);

            int rawType = RawTypeField & 0x3F;
            int lenType = (RawTypeField >> 6) & 0x03;

            switch (lenType)
            {
                case 0:
                    writer.WriteByte((byte)rawType);
                    writer.WriteByte(Version);
                    writer.WriteByte((byte)Length1);
                    writer.WriteByte((byte)Length2);
                    break;
                case 1:
                    writer.WriteByte((byte)(rawType | 0x40));
                    writer.WriteByte(Version);
                    writer.WriteUInt16((ushort)Length1);
                    writer.WriteUInt16((ushort)Length2);
                    break;
                default:
                    writer.WriteByte((byte)(rawType | 0x80));
                    writer.WriteByte(Version);
                    writer.WriteInt32(Length1);
                    writer.WriteInt32(Length2);
                    break;
            }

            writer.WriteBytes(RawData);
        }

        public int CalculateSize()
        {
            int sum = 3 + 4 + 2;
            int lenType = (RawTypeField >> 6) & 0x03;
            if (lenType == 0) sum += 2;
            else if (lenType == 1) sum += 4;
            else sum += 8;
            sum += Length1;
            return sum;
        }

        private static string ObterCodigoTipo(GameType game, int code)
        {
            string[] skyrim =
            {
                "REFR","ACHR","PMIS","PGRE","PBEA","PFLA","CELL","INFO","QUST","NPC_",
                "ACTI","TACT","ARMO","BOOK","CONT","DOOR","INGR","LIGH","MISC","APPA",
                "STAT","MSTT","FURN","WEAP","AMMO","KEYM","ALCH","IDLM","NOTE","ECZN",
                "CLAS","FACT","PACK","NAVM","WOOP","MGEF","SMQN","SCEN","LCTN","RELA",
                "PHZD","PBAR","PCON","FLST","LVLN","LVLI","LVSP","PARW","ENCH"
            };
            if (code >= 0 && code < skyrim.Length) return skyrim[code];
            if (code == 63) return "LAND";
            return "UNK_" + code;
        }

        private static string ObterDescricaoTipo(string code)
        {
            switch (code)
            {
                case "REFR": return "Object Reference";
                case "ACHR": return "NPC Reference";
                case "CELL": return "Cell";
                case "INFO": return "Dialogue Info";
                case "QUST": return "Quest";
                case "NPC_": return "NPC Template";
                case "ACTI": return "Activator";
                case "ARMO": return "Armor";
                case "BOOK": return "Book";
                case "CONT": return "Container";
                case "DOOR": return "Door";
                case "WEAP": return "Weapon";
                case "FACT": return "Faction";
                case "PACK": return "AI Package";
                case "FLST": return "FormList";
                case "LVLN": return "Leveled NPC";
                case "LVLI": return "Leveled Item";
                case "LVSP": return "Leveled Spell";
                case "ENCH": return "Enchantment";
                case "LCTN": return "Location";
                default: return code;
            }
        }
    }
}
