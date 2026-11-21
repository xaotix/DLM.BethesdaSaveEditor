using System;
using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class MiscStatEntry : ObservableBase
    {
        private int _value;

        public WString Name { get; }
        public byte Category { get; }

        public string NameText => Name.Text;

        public string CategoryName
        {
            get
            {
                switch (Category)
                {
                    case 0: return "General";
                    case 1: return "Quest";
                    case 2: return "Combat";
                    case 3: return "Magic";
                    case 4: return "Crafting";
                    case 5: return "Crime";
                    default: return "Other (" + Category + ")";
                }
            }
        }

        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public MiscStatEntry(BinaryReaderEx reader)
        {
            Name = WString.Read(reader);
            Category = reader.ReadByte();
            _value = reader.ReadInt32();
        }

        public void Write(BinaryWriterEx writer)
        {
            Name.Write(writer);
            writer.WriteByte(Category);
            writer.WriteInt32(_value);
        }

        public int CalculateSize() => Name.CalculateSize() + 1 + 4;
    }

    public sealed class MiscStatsBlock : IGlobalDataBlock
    {
        public List<MiscStatEntry> Entries { get; }

        public MiscStatsBlock()
        {
            Entries = new List<MiscStatEntry>();
        }

        public MiscStatsBlock(BinaryReaderEx reader)
        {
            int count = reader.ReadInt32();
            Entries = new List<MiscStatEntry>(count);
            for (int i = 0; i < count; i++)
            {
                Entries.Add(new MiscStatEntry(reader));
            }
        }

        public int ClearAllBounties()
        {
            int cleared = 0;
            foreach (MiscStatEntry entry in Entries)
            {
                if (entry.Category == 5 && entry.NameText.IndexOf("Bounty", StringComparison.OrdinalIgnoreCase) >= 0 && entry.Value > 0)
                {
                    entry.Value = 0;
                    cleared++;
                }
            }
            return cleared;
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteInt32(Entries.Count);
            foreach (MiscStatEntry e in Entries)
            {
                e.Write(writer);
            }
        }

        public int CalculateSize() => 4 + Entries.Sum(e => e.CalculateSize());
    }
}

