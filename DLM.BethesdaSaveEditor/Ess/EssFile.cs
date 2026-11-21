using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;
using DLM.BethesdaSaveEditor.Ess.Papyrus;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class SaveHealthReport
    {
        public int Pontuacao { get; set; }
        public string NivelSaude { get; set; }
        public List<string> Alertas { get; } = new List<string>();
        public int InstanciasSoltas { get; set; }
        public int ElementosIndefinidos { get; set; }
        public int ThreadsIndefinidas { get; set; }
        public int InstanciasSemForm { get; set; }
        public int TotalStrings { get; set; }
        public bool AlertaLimiteStrings { get; set; }
        public double PercentualPapyrus { get; set; }
        public double PercentualChangeForms { get; set; }
    }

    public sealed class EssFile
    {
        private readonly Dictionary<int, RefID> _refIdCache = new Dictionary<int, RefID>(65536);

        public string FilePath { get; private set; }
        public long FileSizeOnDisk { get; private set; }
        public SaveHeader Header { get; private set; }
        public byte FormVersion { get; private set; }
        public WString VersionString { get; private set; }
        public PluginInfo Plugins { get; private set; }
        public FileLocationTable LocationTable { get; private set; }
        public List<GlobalData> Table1 { get; private set; }
        public List<GlobalData> Table2 { get; private set; }
        public List<ChangeForm> ChangeForms { get; private set; }
        public List<GlobalData> Table3 { get; private set; }
        public int[] FormIDArray { get; private set; }
        public int[] VisitedWorldspaces { get; private set; }
        public byte[] Unknown3 { get; private set; }
        public byte[] CosaveBytes { get; private set; }

        public MiscStatsBlock MiscStats { get; private set; }
        public PlayerLocationBlock PlayerLocation { get; private set; }
        public GlobalVariableTable GlobalVariables { get; private set; }
        public PapyrusBlock Papyrus { get; private set; }

        public bool IsTruncated { get; private set; }
        public bool HasChanges { get; set; }
        public List<string> Avisos { get; } = new List<string>();

        public bool SupportsEsl
        {
            get
            {
                if (Header.Game.EhFallout4()) return FormVersion >= 68;
                if (Header.Game == GameType.SkyrimLE) return false;
                return FormVersion >= 78;
            }
        }

        public bool IsStr32
        {
            get
            {
                if (Header.Game.EhFallout4()) return FormVersion > 61;
                if (Header.Game == GameType.SkyrimLE) return false;
                return true;
            }
        }

        public bool HasCosave => CosaveBytes != null && CosaveBytes.Length > 0;
        public bool IsBroken => IsTruncated || (Plugins != null && Plugins.HasOverflow) || (Papyrus != null && Papyrus.IsBroken);

        public void AdicionarAviso(string msg)
        {
            Avisos.Add(msg);
        }

        public static SaveHeader ReadHeaderOnly(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int toRead = (int)Math.Min(fs.Length, 4 * 1024 * 1024);
                byte[] buf = new byte[toRead];
                int read = fs.Read(buf, 0, toRead);
                BinaryReaderEx reader = new BinaryReaderEx(buf, 0, read);
                return new SaveHeader(reader, path);
            }
        }

        public static EssFile Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("Save file not found.", path);
            }

            byte[] allBytes = File.ReadAllBytes(path);
            EssFile ess = new EssFile();
            ess.FilePath = path;
            ess.FileSizeOnDisk = allBytes.Length;
            ess.Parse(allBytes);
            return ess;
        }

        private void Parse(byte[] allBytes)
        {
            BinaryReaderEx rawReader = new BinaryReaderEx(allBytes);
            Header = new SaveHeader(rawReader, FilePath);
            int headerEndOffset = rawReader.Position;

            string cosavePath = Path.ChangeExtension(FilePath, Header.Game.ObterExtensaoCosave());
            if (File.Exists(cosavePath))
            {
                CosaveBytes = File.ReadAllBytes(cosavePath);
            }

            byte[] bodyBytes;
            if (Header.Compression == CompressionType.Uncompressed)
            {
                bodyBytes = rawReader.ReadBytes(rawReader.Remaining);
            }
            else
            {
                int uncompressedLen = rawReader.ReadInt32();
                int compressedLen = rawReader.ReadInt32();
                if (uncompressedLen < 0 || compressedLen < 0)
                {
                    throw new InvalidDataException("Invalid compression header.");
                }
                byte[] compressed = rawReader.ReadBytes(compressedLen);

                if (Header.Compression == CompressionType.Zlib)
                {
                    bodyBytes = CompressionHelper.InflateZlib(compressed, uncompressedLen);
                }
                else if (Header.Compression == CompressionType.Lz4)
                {
                    bodyBytes = CompressionHelper.DecompressLz4(compressed, uncompressedLen);
                }
                else
                {
                    throw new InvalidDataException("Unsupported compression type: " + Header.Compression);
                }
            }

            BinaryReaderEx body = new BinaryReaderEx(bodyBytes);
            FormVersion = body.ReadByte();
            if (Header.Game.EhFallout4())
            {
                VersionString = WString.Read(body);
            }

            Plugins = new PluginInfo(body, SupportsEsl);
            LocationTable = new FileLocationTable(body, Header.Game);

            try
            {
                int formIdOffsetInBody = LocationTable.FormIDArrayCountOffset - headerEndOffset;
                body.Position = formIdOffsetInBody;
                int formIdCount = body.ReadInt32();
                FormIDArray = new int[formIdCount];
                for (int i = 0; i < formIdCount; i++)
                {
                    FormIDArray[i] = body.ReadInt32();
                }
            }
            catch (Exception ex)
            {
                IsTruncated = true;
                FormIDArray = new int[0];
                AdicionarAviso("Truncated FormID table: " + ex.Message);
            }

            body.Position = LocationTable.Table1Offset - headerEndOffset;

            Table1 = new List<GlobalData>(LocationTable.Table1Count);
            for (int i = 0; i < LocationTable.Table1Count; i++)
            {
                Table1.Add(new GlobalData(body, this));
            }

            Table2 = new List<GlobalData>(LocationTable.Table2Count);
            for (int i = 0; i < LocationTable.Table2Count; i++)
            {
                Table2.Add(new GlobalData(body, this));
            }

            GlobalData msData = Table1.FirstOrDefault(t => t.Type == 0 && t.Block is MiscStatsBlock);
            MiscStats = msData != null ? (MiscStatsBlock)msData.Block : new MiscStatsBlock();

            GlobalData plData = Table1.FirstOrDefault(t => t.Type == 1 && t.Block is PlayerLocationBlock);
            PlayerLocation = plData != null ? (PlayerLocationBlock)plData.Block : null;

            GlobalData gvData = Table1.FirstOrDefault(t => t.Type == 3 && t.Block is GlobalVariableTable);
            GlobalVariables = gvData != null ? (GlobalVariableTable)gvData.Block : new GlobalVariableTable();

            ChangeForms = new List<ChangeForm>(LocationTable.ChangeFormCount);
            for (int i = 0; i < LocationTable.ChangeFormCount; i++)
            {
                ChangeForms.Add(new ChangeForm(body, this));
            }

            Table3 = new List<GlobalData>(LocationTable.Table3Count);
            for (int i = 0; i < LocationTable.Table3Count; i++)
            {
                if (body.Remaining < 8)
                {
                    IsTruncated = true;
                    AdicionarAviso("Table3 truncated at index " + i);
                    break;
                }
                Table3.Add(new GlobalData(body, this));
            }

            GlobalData papData = Table3.FirstOrDefault(t => t.Type == 1001 && t.Block is PapyrusBlock);
            Papyrus = papData != null ? (PapyrusBlock)papData.Block : null;

            try
            {
                int skipFormId = (LocationTable.FormIDArrayCountOffset - headerEndOffset) + 4 + (4 * FormIDArray.Length);
                if (skipFormId >= 0 && skipFormId <= body.Length)
                {
                    body.Position = skipFormId;
                    int wsCount = body.ReadInt32();
                    VisitedWorldspaces = new int[wsCount];
                    for (int i = 0; i < wsCount; i++)
                    {
                        VisitedWorldspaces[i] = body.ReadInt32();
                    }
                    Unknown3 = body.ReadBytes(body.Remaining);
                }
                else
                {
                    VisitedWorldspaces = new int[0];
                    Unknown3 = new byte[0];
                }
            }
            catch
            {
                IsTruncated = true;
                VisitedWorldspaces = new int[0];
                Unknown3 = new byte[0];
            }
        }

        public string Save(string targetPath, bool createBackup = true)
        {
            if (IsBroken)
            {
                throw new InvalidOperationException("This save file is truncated or corrupted and cannot be safely saved.");
            }

            string backupPath = null;
            if (createBackup && File.Exists(targetPath))
            {
                backupPath = CreateBackup(targetPath);
            }

            LocationTable.Rebuild(this);

            byte[] headerBytes;
            using (BinaryWriterEx hw = new BinaryWriterEx(Header.CalculateSize()))
            {
                Header.Write(hw);
                headerBytes = hw.ToArray();
            }

            byte[] bodyBytes;
            using (BinaryWriterEx bw = new BinaryWriterEx(Math.Max(65536, CalculateBodySize())))
            {
                bw.WriteByte(FormVersion);
                if (VersionString != null) VersionString.Write(bw);
                Plugins.Write(bw);
                LocationTable.Write(bw);

                foreach (GlobalData gd in Table1) gd.Write(bw);
                foreach (GlobalData gd in Table2) gd.Write(bw);
                foreach (ChangeForm cf in ChangeForms) cf.Write(bw);
                foreach (GlobalData gd in Table3) gd.Write(bw);

                bw.WriteInt32(FormIDArray.Length);
                for (int i = 0; i < FormIDArray.Length; i++) bw.WriteInt32(FormIDArray[i]);

                bw.WriteInt32(VisitedWorldspaces.Length);
                for (int i = 0; i < VisitedWorldspaces.Length; i++) bw.WriteInt32(VisitedWorldspaces[i]);

                bw.WriteBytes(Unknown3);
                bodyBytes = bw.ToArray();
            }

            using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(headerBytes, 0, headerBytes.Length);

                if (Header.Compression == CompressionType.Uncompressed)
                {
                    fs.Write(bodyBytes, 0, bodyBytes.Length);
                }
                else
                {
                    byte[] compressed = Header.Compression == CompressionType.Lz4
                        ? CompressionHelper.CompressLz4(bodyBytes)
                        : CompressionHelper.DeflateZlib(bodyBytes);

                    byte[] prefix = new byte[8];
                    Buffer.BlockCopy(BitConverter.GetBytes(bodyBytes.Length), 0, prefix, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(compressed.Length), 0, prefix, 4, 4);
                    fs.Write(prefix, 0, 8);
                    fs.Write(compressed, 0, compressed.Length);
                }
            }

            if (HasCosave)
            {
                string cosaveTarget = Path.ChangeExtension(targetPath, Header.Game.ObterExtensaoCosave());
                if (createBackup && File.Exists(cosaveTarget)) CreateBackup(cosaveTarget);
                File.WriteAllBytes(cosaveTarget, CosaveBytes);
            }

            FilePath = targetPath;
            FileSizeOnDisk = new FileInfo(targetPath).Length;
            HasChanges = false;
            return backupPath;
        }

        private static string CreateBackup(string path)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backup = path + "." + stamp + ".bak";
            File.Copy(path, backup, true);
            return backup;
        }

        public RefID ReadRefID(BinaryReaderEx reader)
        {
            int raw = reader.ReadRefIDRaw();
            RefID cached;
            if (!_refIdCache.TryGetValue(raw, out cached))
            {
                cached = new RefID(raw, this, Plugins != null && Plugins.HasOverflow);
                _refIdCache[raw] = cached;
            }
            return cached;
        }

        public Plugin GetPluginFor(int formID)
        {
            if (Plugins == null) return null;
            uint uForm = (uint)formID;
            int idx = (int)(uForm >> 24);
            int subIdx = (int)((uForm & 0xFFFFFF) >> 12);

            if (idx < 0xFE && idx < Plugins.FullPlugins.Count) return Plugins.FullPlugins[idx];
            if (idx == 0xFE && Plugins.HasLiteTable && subIdx < Plugins.LitePlugins.Count) return Plugins.LitePlugins[subIdx];
            return null;
        }

        public int CalculateBodySize()
        {
            int sum = 1;
            if (VersionString != null) sum += VersionString.CalculateSize();
            sum += Plugins.CalculateSize();
            sum += LocationTable.CalculateSize();
            sum += Table1.Sum(t => t.CalculateSize());
            sum += Table2.Sum(t => t.CalculateSize());
            sum += ChangeForms.Sum(cf => cf.CalculateSize());
            sum += Table3.Sum(t => t.CalculateSize());
            sum += 4 + (FormIDArray != null ? FormIDArray.Length * 4 : 0);
            sum += 4 + (VisitedWorldspaces != null ? VisitedWorldspaces.Length * 4 : 0);
            sum += Unknown3 != null ? Unknown3.Length : 0;
            return sum;
        }

        public int CalculateTotalUncompressedSize() => Header.CalculateSize() + CalculateBodySize();

        public int RemoveUnattachedInstances()
        {
            if (Papyrus == null) return 0;
            int removed = Papyrus.RemoveUnattachedInstances();
            if (removed > 0) HasChanges = true;
            return removed;
        }

        public int RemoveUndefinedElements()
        {
            if (Papyrus == null) return 0;
            int removed = Papyrus.RemoveUndefinedElements();
            if (removed > 0) HasChanges = true;
            return removed;
        }

        public int RemoveNonExistentCreatedInstances()
        {
            if (Papyrus == null) return 0;
            HashSet<RefID> existingForms = new HashSet<RefID>(ChangeForms.Select(cf => cf.RefID));
            int removed = Papyrus.RemoveNonExistentCreatedInstances(existingForms);
            if (removed > 0) HasChanges = true;
            return removed;
        }

        public int[] PurifyFormLists()
        {
            int totalEntries = 0;
            int formsCleaned = 0;

            foreach (ChangeForm cf in ChangeForms)
            {
                if (cf.TipoCodigo == "FLST")
                {
                    int rem = cf.PurifyFormList(this);
                    if (rem > 0)
                    {
                        totalEntries += rem;
                        formsCleaned++;
                    }
                }
            }

            if (totalEntries > 0) HasChanges = true;
            return new int[] { totalEntries, formsCleaned };
        }

        public SaveHealthReport AnalyzeHealth()
        {
            SaveHealthReport report = new SaveHealthReport();
            int score = 100;

            double totalSize = Math.Max(1, CalculateTotalUncompressedSize());
            double cfBytes = ChangeForms.Sum(c => (double)c.CalculateSize());
            double papBytes = Papyrus != null ? Papyrus.CalculateSize() : 0;
            report.PercentualChangeForms = (cfBytes / totalSize) * 100.0;
            report.PercentualPapyrus = (papBytes / totalSize) * 100.0;

            if (IsTruncated)
            {
                score -= 50;
                report.Alertas.Add("CRITICAL: Save file is truncated.");
            }
            if (Plugins.HasOverflow)
            {
                score -= 40;
                report.Alertas.Add("CRITICAL: Plugin table overflow (> 255 full plugins).");
            }

            if (Papyrus != null)
            {
                report.InstanciasSoltas = Papyrus.CountUnattachedInstances();
                int[] undef = Papyrus.CountUndefinedElements();
                report.ElementosIndefinidos = undef[0];
                report.ThreadsIndefinidas = undef[1];
                report.TotalStrings = Papyrus.Strings.Count;

                HashSet<RefID> existingForms = new HashSet<RefID>(ChangeForms.Select(cf => cf.RefID));
                report.InstanciasSemForm = Papyrus.CountNonExistentCreatedInstances(existingForms);

                if (report.InstanciasSoltas > 0)
                {
                    score -= Math.Min(25, 5 + report.InstanciasSoltas / 10);
                    report.Alertas.Add(string.Format("{0} unattached script instance(s).", report.InstanciasSoltas));
                }
                if (report.ElementosIndefinidos > 0)
                {
                    score -= Math.Min(25, 5 + report.ElementosIndefinidos / 10);
                    report.Alertas.Add(string.Format("{0} undefined Papyrus element(s).", report.ElementosIndefinidos));
                }
                if (report.ThreadsIndefinidas > 0)
                {
                    score -= Math.Min(20, 10 + report.ThreadsIndefinidas * 2);
                    report.Alertas.Add(string.Format("{0} undefined active thread(s).", report.ThreadsIndefinidas));
                }
                if (report.InstanciasSemForm > 0)
                {
                    score -= Math.Min(15, 3 + report.InstanciasSemForm / 20);
                    report.Alertas.Add(string.Format("{0} instance(s) pointing to non-existent created forms.", report.InstanciasSemForm));
                }

                if (!IsStr32 && report.TotalStrings > 55000)
                {
                    report.AlertaLimiteStrings = true;
                    score -= 15;
                    report.Alertas.Add(string.Format("String table near 65,535 limit ({0:N0} strings).", report.TotalStrings));
                }

                if (Papyrus.ActiveScripts.Count > 50)
                {
                    score -= 10;
                    report.Alertas.Add(string.Format("High active script thread count ({0}).", Papyrus.ActiveScripts.Count));
                }
            }

            if (Plugins.FullPlugins.Count >= 245)
            {
                score -= 10;
                report.Alertas.Add(string.Format("Near 254 full plugin limit ({0}/254).", Plugins.FullPlugins.Count));
            }

            if (score < 0) score = 0;
            report.Pontuacao = score;

            if (score >= 85) report.NivelSaude = "Excellent";
            else if (score >= 65) report.NivelSaude = "Good (clean recommended)";
            else if (score >= 40) report.NivelSaude = "Warning (save bloated)";
            else report.NivelSaude = "Critical";

            return report;
        }
    }
}
