using System;
using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class PluginInfo
    {
        public static readonly Plugin Created = Plugin.CreatePseudoCreated();

        public List<Plugin> FullPlugins { get; }
        public List<Plugin> LitePlugins { get; }
        public bool HasLiteTable { get; }
        public bool HasOverflow { get; }

        public int TotalCount => FullPlugins.Count + (LitePlugins != null ? LitePlugins.Count : 0);

        public PluginInfo(BinaryReaderEx reader, bool supportsEsl)
        {
            int pluginInfoSize = reader.ReadInt32();
            int startPos = reader.Position;

            int numFull = reader.ReadByte();
            FullPlugins = new List<Plugin>(numFull);
            for (int i = 0; i < numFull; i++)
            {
                FullPlugins.Add(Plugin.ReadFull(reader, i));
            }

            if (supportsEsl)
            {
                HasLiteTable = true;
                int numLite = reader.ReadUInt16();
                LitePlugins = new List<Plugin>(numLite);
                for (int i = 0; i < numLite; i++)
                {
                    LitePlugins.Add(Plugin.ReadLite(reader, i));
                }
            }
            else
            {
                HasLiteTable = false;
                LitePlugins = new List<Plugin>(0);
            }

            int bytesRead = reader.Position - startPos;
            if (bytesRead != pluginInfoSize)
            {
                // Ajusta a posicao se houver diferenca de alinhamento
                reader.Position = startPos + pluginInfoSize;
            }

            HasOverflow = numFull < 1;
        }

        public IEnumerable<Plugin> AllPlugins()
        {
            foreach (Plugin p in FullPlugins)
            {
                yield return p;
            }
            if (LitePlugins != null)
            {
                foreach (Plugin p in LitePlugins)
                {
                    yield return p;
                }
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteInt32(CalculateSize() - 4);
            writer.WriteByte((byte)FullPlugins.Count);
            foreach (Plugin p in FullPlugins)
            {
                p.Write(writer);
            }

            if (HasLiteTable)
            {
                writer.WriteUInt16((ushort)LitePlugins.Count);
                foreach (Plugin p in LitePlugins)
                {
                    p.Write(writer);
                }
            }
        }

        public int CalculateSize()
        {
            int sum = 4 + 1;
            sum += FullPlugins.Sum(p => p.CalculateSize());
            if (HasLiteTable)
            {
                sum += 2;
                sum += LitePlugins.Sum(p => p.CalculateSize());
            }
            return sum;
        }
    }
}

