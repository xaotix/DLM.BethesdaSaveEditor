using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class SaveHeader
    {
        public byte[] Magic { get; }
        public GameType Game { get; }
        public int Version { get; }
        public int SaveNumber { get; set; }
        public WString CharacterName { get; }
        public int Level { get; set; }
        public WString Location { get; }
        public WString GameDate { get; }
        public WString RaceId { get; }
        public short Sex { get; set; }
        public float CurrentXp { get; set; }
        public float NeededXp { get; set; }
        public long FileTime { get; }
        public int ScreenshotWidth { get; }
        public int ScreenshotHeight { get; }
        public int BytesPerPixel { get; }
        public byte[] ScreenshotData { get; }

        private CompressionType? _compression;
        public CompressionType Compression
        {
            get => _compression.HasValue ? _compression.Value : CompressionType.Uncompressed;
            set
            {
                if (Game.SuportaCompressao())
                {
                    _compression = value;
                }
            }
        }

        public bool HasCompressionField => _compression.HasValue;

        public string GeneroTexto => Sex == 0 ? "Masculino" : "Feminino";
        public string RacaLimpa => (RaceId != null ? RaceId.Text : string.Empty).Replace("Race", string.Empty);

        public DateTime DataReal
        {
            get
            {
                try
                {
                    return DateTime.FromFileTime(FileTime);
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
        }

        public SaveHeader(BinaryReaderEx reader, string filePath)
        {
            int startPos = reader.Position;
            string prefix = reader.ReadFixedAscii(4);
            reader.Position = startPos;

            switch (prefix)
            {
                case "TESV":
                    Magic = reader.ReadBytes(13);
                    break;
                case "FO4_":
                case "TES4":
                    Magic = reader.ReadBytes(12);
                    break;
                default:
                    throw new InvalidDataException("Assinatura de arquivo de save nao reconhecida: " + prefix);
            }

            int headerSize = reader.ReadInt32();
            if (headerSize <= 0 || headerSize >= 4096)
            {
                throw new InvalidDataException("Tamanho de cabecalho invalido: " + headerSize);
            }

            Version = reader.ReadInt32();
            string magicStr = Encoding.ASCII.GetString(Magic).ToUpperInvariant();
            string ext = (Path.GetExtension(filePath ?? string.Empty) ?? string.Empty).TrimStart('.').ToLowerInvariant();

            if (magicStr == "TESV_SAVEGAME")
            {
                if (Version <= 9)
                {
                    Game = GameType.SkyrimLE;
                }
                else if (ext == "sav0")
                {
                    Game = GameType.SkyrimSW;
                }
                else
                {
                    Game = GameType.SkyrimSE;
                }
            }
            else if (magicStr == "FO4_SAVEGAME")
            {
                Game = GameType.Fallout4;
            }
            else
            {
                throw new InvalidDataException("Jogo nao suportado: " + magicStr);
            }

            SaveNumber = reader.ReadInt32();
            CharacterName = WString.Read(reader);
            Level = reader.ReadInt32();
            Location = WString.Read(reader);
            GameDate = WString.Read(reader);
            RaceId = WString.Read(reader);
            Sex = reader.ReadInt16();
            CurrentXp = reader.ReadSingle();
            NeededXp = reader.ReadSingle();
            FileTime = reader.ReadInt64();
            ScreenshotWidth = reader.ReadInt32();
            ScreenshotHeight = reader.ReadInt32();

            if (Game == GameType.SkyrimSE || Game == GameType.SkyrimSW || Game == GameType.SkyrimVR)
            {
                _compression = (CompressionType)reader.ReadInt16();
            }
            else
            {
                _compression = null;
            }

            BytesPerPixel = Game == GameType.SkyrimLE ? 3 : 4;
            int shotBytes = BytesPerPixel * ScreenshotWidth * ScreenshotHeight;
            if (shotBytes < 0 || shotBytes > 64 * 1024 * 1024)
            {
                throw new InvalidDataException("Dimensoes de captura invalidas: " + ScreenshotWidth + "x" + ScreenshotHeight);
            }
            ScreenshotData = reader.ReadBytes(shotBytes);
        }

        public int PartialSize()
        {
            int sum = 4 + 4; // Version + SaveNumber
            sum += CharacterName.CalculateSize();
            sum += 4; // Level
            sum += Location.CalculateSize();
            sum += GameDate.CalculateSize();
            sum += RaceId.CalculateSize();
            sum += 2 + 4 + 4 + 8 + 8; // Sex + CurrentXP + NeededXP + FileTime + W/H
            sum += _compression.HasValue ? 2 : 0;
            return sum;
        }

        public int CalculateSize()
        {
            return Magic.Length + 4 + PartialSize() + ScreenshotData.Length;
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteBytes(Magic);
            writer.WriteInt32(PartialSize());
            writer.WriteInt32(Version);
            writer.WriteInt32(SaveNumber);
            CharacterName.Write(writer);
            writer.WriteInt32(Level);
            Location.Write(writer);
            GameDate.Write(writer);
            RaceId.Write(writer);
            writer.WriteInt16(Sex);
            writer.WriteSingle(CurrentXp);
            writer.WriteSingle(NeededXp);
            writer.WriteInt64(FileTime);
            writer.WriteInt32(ScreenshotWidth);
            writer.WriteInt32(ScreenshotHeight);
            if (_compression.HasValue)
            {
                writer.WriteInt16((short)_compression.Value);
            }
            writer.WriteBytes(ScreenshotData);
        }

        public BitmapSource CreateScreenshotBitmap()
        {
            if (ScreenshotData == null || ScreenshotData.Length < 10 || ScreenshotWidth <= 0 || ScreenshotHeight <= 0)
            {
                return null;
            }

            int stride = ScreenshotWidth * 4;
            byte[] bgra = new byte[stride * ScreenshotHeight];
            int srcIdx = 0;
            int dstIdx = 0;
            int totalPixels = ScreenshotWidth * ScreenshotHeight;

            for (int i = 0; i < totalPixels; i++)
            {
                byte r = ScreenshotData[srcIdx];
                byte g = ScreenshotData[srcIdx + 1];
                byte b = ScreenshotData[srcIdx + 2];
                bgra[dstIdx] = b;
                bgra[dstIdx + 1] = g;
                bgra[dstIdx + 2] = r;
                bgra[dstIdx + 3] = 255;
                srcIdx += BytesPerPixel;
                dstIdx += 4;
            }

            WriteableBitmap bmp = new WriteableBitmap(ScreenshotWidth, ScreenshotHeight, 96, 96, PixelFormats.Bgra32, null);
            bmp.WritePixels(new Int32Rect(0, 0, ScreenshotWidth, ScreenshotHeight), bgra, stride, 0);
            bmp.Freeze();
            return bmp;
        }

        public void ExportScreenshotPng(string outputPath)
        {
            BitmapSource bmp = CreateScreenshotBitmap();
            if (bmp == null)
            {
                throw new InvalidOperationException("Este save nao possui captura de tela embutida.");
            }

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fs);
            }
        }
    }
}

