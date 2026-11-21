using System;
using DLM.BethesdaSaveEditor.Core;
using DLM.BethesdaSaveEditor.Ess.Papyrus;

namespace DLM.BethesdaSaveEditor.Ess
{
    public interface IGlobalDataBlock
    {
        void Write(BinaryWriterEx writer);
        int CalculateSize();
    }

    public sealed class RawGlobalDataBlock : IGlobalDataBlock
    {
        public byte[] Data { get; }

        public RawGlobalDataBlock(byte[] data)
        {
            Data = data ?? new byte[0];
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteBytes(Data);
        }

        public int CalculateSize() => Data.Length;
    }

    public sealed class GlobalData
    {
        public int Type { get; }
        public IGlobalDataBlock Block { get; }
        public byte[] RawBytesFallback { get; }

        public string NomeTipo
        {
            get
            {
                switch (Type)
                {
                    case 0: return "Misc Stats";
                    case 1: return "Player Location";
                    case 2: return "TES";
                    case 3: return "Global Variables";
                    case 4: return "Created Objects";
                    case 5: return "Effects";
                    case 6: return "Weather";
                    case 7: return "Audio";
                    case 8: return "SkyCells";
                    case 100: return "Process Lists";
                    case 101: return "Combat";
                    case 102: return "Interface";
                    case 103: return "Actor Causes";
                    case 104: return "Unknown 104";
                    case 105: return "Detection Manager";
                    case 106: return "Location MetaData";
                    case 107: return "Quest Static Data";
                    case 108: return "StoryTeller";
                    case 109: return "Magic Favorites";
                    case 110: return "PlayerControls";
                    case 111: return "Story Event Manager";
                    case 112: return "Ingredient Shared";
                    case 113: return "Menu Controls";
                    case 114: return "Menu Topic Manager";
                    case 1000: return "Temp Effects";
                    case 1001: return "Papyrus";
                    case 1002: return "Anim Objects";
                    case 1003: return "Timer";
                    case 1004: return "Synchronized Animations";
                    case 1005: return "Main";
                    default: return "GlobalData #" + Type;
                }
            }
        }

        public GlobalData(BinaryReaderEx reader, EssFile ess)
        {
            Type = reader.ReadInt32();
            int blockSize = reader.ReadInt32();
            BinaryReaderEx sub = reader.Slice(blockSize);

            if (Type == 0)
            {
                try
                {
                    MiscStatsBlock ms = new MiscStatsBlock(sub);
                    Block = ms.CalculateSize() == blockSize ? (IGlobalDataBlock)ms : ReadRaw(sub, blockSize);
                }
                catch
                {
                    Block = ReadRaw(sub, blockSize);
                }
            }
            else if (Type == 1 && blockSize >= 30)
            {
                try
                {
                    PlayerLocationBlock pl = new PlayerLocationBlock(sub, ess);
                    Block = pl.CalculateSize() == blockSize ? (IGlobalDataBlock)pl : ReadRaw(sub, blockSize);
                }
                catch
                {
                    Block = ReadRaw(sub, blockSize);
                }
            }
            else if (Type == 3)
            {
                try
                {
                    GlobalVariableTable gv = new GlobalVariableTable(sub, ess);
                    Block = gv.CalculateSize() == blockSize ? (IGlobalDataBlock)gv : ReadRaw(sub, blockSize);
                }
                catch
                {
                    Block = ReadRaw(sub, blockSize);
                }
            }
            else if (Type == 1001)
            {
                RawBytesFallback = null;
                try
                {
                    Block = new PapyrusBlock(sub, ess);
                }
                catch (Exception ex)
                {
                    ess.AdicionarAviso("Papyrus parse error (preserved as raw bytes): " + ex.Message);
                    RawGlobalDataBlock raw = ReadRaw(sub, blockSize);
                    RawBytesFallback = raw.Data;
                    Block = raw;
                }
            }
            else
            {
                Block = new RawGlobalDataBlock(sub.ReadBytes(blockSize));
            }
        }

        private static RawGlobalDataBlock ReadRaw(BinaryReaderEx sub, int blockSize)
        {
            sub.Position = 0;
            return new RawGlobalDataBlock(sub.ReadBytes(blockSize));
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteInt32(Type);
            writer.WriteInt32(Block.CalculateSize());
            Block.Write(writer);
        }

        public int CalculateSize() => 8 + Block.CalculateSize();
    }
}
