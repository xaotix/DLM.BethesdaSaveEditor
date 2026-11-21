using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class FileLocationTable
    {
        public int FormIDArrayCountOffset { get; set; }
        public int UnknownTable3Offset { get; set; }
        public int Table1Offset { get; set; }
        public int Table2Offset { get; set; }
        public int ChangeFormsOffset { get; set; }
        public int Table3Offset { get; set; }
        public int Table1Count { get; set; }
        public int Table2Count { get; set; }
        public int Table3Count { get; set; }
        public int ChangeFormCount { get; set; }
        public int[] Unused { get; }
        public GameType Game { get; }

        public FileLocationTable(BinaryReaderEx reader, GameType game)
        {
            Game = game;
            FormIDArrayCountOffset = reader.ReadInt32();
            UnknownTable3Offset = reader.ReadInt32();
            Table1Offset = reader.ReadInt32();
            Table2Offset = reader.ReadInt32();
            ChangeFormsOffset = reader.ReadInt32();
            Table3Offset = reader.ReadInt32();
            Table1Count = reader.ReadInt32();
            Table2Count = reader.ReadInt32();
            int rawT3 = reader.ReadInt32();
            Table3Count = game.EhSkyrim() ? (rawT3 + 1) : rawT3;
            ChangeFormCount = reader.ReadInt32();

            Unused = new int[15];
            for (int i = 0; i < 15; i++)
            {
                Unused[i] = reader.ReadInt32();
            }
        }

        public void Rebuild(EssFile ess)
        {
            int t1Size = ess.Table1.Sum(t => t.CalculateSize());
            int t2Size = ess.Table2.Sum(t => t.CalculateSize());
            int t3Size = ess.Table3.Sum(t => t.CalculateSize());
            int cfSize = ess.ChangeForms.Sum(cf => cf.CalculateSize());

            Table1Count = ess.Table1.Count;
            Table2Count = ess.Table2.Count;
            Table3Count = ess.Table3.Count;
            ChangeFormCount = ess.ChangeForms.Count;

            int offset = ess.Header.CalculateSize();
            offset += 1; // FormVersion
            if (ess.VersionString != null)
            {
                offset += ess.VersionString.CalculateSize();
            }
            offset += ess.Plugins.CalculateSize();
            offset += CalculateSize();

            Table1Offset = offset;
            Table2Offset = Table1Offset + t1Size;
            ChangeFormsOffset = Table2Offset + t2Size;
            Table3Offset = ChangeFormsOffset + cfSize;
            FormIDArrayCountOffset = Table3Offset + t3Size;

            int formIdBytes = 4 + 4 * (ess.FormIDArray != null ? ess.FormIDArray.Length : 0);
            int worldBytes = 4 + 4 * (ess.VisitedWorldspaces != null ? ess.VisitedWorldspaces.Length : 0);
            UnknownTable3Offset = FormIDArrayCountOffset + formIdBytes + worldBytes;
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteInt32(FormIDArrayCountOffset);
            writer.WriteInt32(UnknownTable3Offset);
            writer.WriteInt32(Table1Offset);
            writer.WriteInt32(Table2Offset);
            writer.WriteInt32(ChangeFormsOffset);
            writer.WriteInt32(Table3Offset);
            writer.WriteInt32(Table1Count);
            writer.WriteInt32(Table2Count);
            writer.WriteInt32(Game.EhSkyrim() ? (Table3Count - 1) : Table3Count);
            writer.WriteInt32(ChangeFormCount);
            for (int i = 0; i < Unused.Length; i++)
            {
                writer.WriteInt32(Unused[i]);
            }
        }

        public int CalculateSize() => 100;
    }
}

