using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class PlayerLocationBlock : IGlobalDataBlock
    {
        public int NextObjectId { get; set; }
        public RefID Worldspace1 { get; }
        public int CellX { get; set; }
        public int CellY { get; set; }
        public RefID Worldspace2 { get; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public byte[] TrailingBytes { get; }

        public PlayerLocationBlock(BinaryReaderEx reader, EssFile ess)
        {
            NextObjectId = reader.ReadInt32();
            Worldspace1 = ess.ReadRefID(reader);
            CellX = reader.ReadInt32();
            CellY = reader.ReadInt32();
            Worldspace2 = ess.ReadRefID(reader);
            PosX = reader.ReadSingle();
            PosY = reader.ReadSingle();
            PosZ = reader.ReadSingle();
            TrailingBytes = reader.ReadBytes(reader.Remaining);
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteInt32(NextObjectId);
            Worldspace1.Write(writer);
            writer.WriteInt32(CellX);
            writer.WriteInt32(CellY);
            Worldspace2.Write(writer);
            writer.WriteSingle(PosX);
            writer.WriteSingle(PosY);
            writer.WriteSingle(PosZ);
            writer.WriteBytes(TrailingBytes);
        }

        public int CalculateSize() => 4 + 3 + 4 + 4 + 3 + 4 + 4 + 4 + (TrailingBytes != null ? TrailingBytes.Length : 0);
    }
}

