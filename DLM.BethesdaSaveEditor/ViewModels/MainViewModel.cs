using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using DLM.BethesdaSaveEditor.Core;
using DLM.BethesdaSaveEditor.Ess;
using DLM.BethesdaSaveEditor.Ess.Papyrus;

namespace DLM.BethesdaSaveEditor.ViewModels
{
    public sealed class MainViewModel : ObservableBase
    {
        private EssFile _saveAtual;
        private PlayerNpcEditor _playerNpc;
        private bool _carregando;
        private string _statusTexto = "Ready";
        private string _memoriaTexto = string.Empty;
        private string _tituloJanela = "DLM.BethesdaSaveEditor - xaotix";

        private BitmapSource _capturaTela;
        private string _nomePersonagem = "-";
        private string _jogoNome = "-";
        private string _nivelTexto = "-";
        private string _racaGenero = "-";
        private string _localizacao = "-";
        private string _dataJogo = "-";
        private string _tamanhoDiscoTexto = "-";
        private string _tamanhoDescompactadoTexto = "-";
        private string _compressaoTexto = "-";
        private string _statusArquivoTexto = "No file loaded";

        private int _pontuacaoSaude;
        private string _nivelSaude = "-";
        private string _resumoPapyrus = "-";
        private string _resumoChangeForms = "-";
        private string _resumoPlugins = "-";

        // Character Direct Binary Edit fields (ACHR 00000014 + NPC_ 00000007 + Header)
        private int _editLevel;
        private float _editCurrentXp;
        private float _editHealth;
        private float _editSecondaryAttr;
        private float _editTertiaryAttr;
        private float _editCarryWeight;
        private float _editSpeedMult;
        private float _editDragonSouls;
        private int _editCurrency;
        private int _editLockpicks;
        private bool _possuiPlayerNpc;
        private bool _possuiActorValues;
        private bool _possuiCurrency;
        private bool _possuiLockpicks;
        private bool _possuiDragonSouls;
        private bool _isSkyrimSave = true;
        private string _labelSecondaryAttr = "Magicka:";
        private string _labelTertiaryAttr = "Stamina:";
        private string _labelCurrency = "Gold (Septims):";
        private string _labelLockpicks = "Lockpicks:";
        private string _labelSkillsTab = "18 Skills & Legendary";

        // Filters
        private string _filtroPlugins = string.Empty;
        private string _filtroMiscStats = string.Empty;
        private string _filtroScripts = string.Empty;
        private string _filtroInstancias = string.Empty;
        private bool _somenteInstanciasSoltas;
        private bool _somenteInstanciasIndefinidas;
        private string _filtroArrays = string.Empty;
        private string _filtroStrings = string.Empty;
        private string _filtroChangeForms = string.Empty;
        private string _filtroGlobais = string.Empty;
        private string _filtroQuests = string.Empty;

        private GlobalPropertyPreset _globalPresetSelecionado;
        private QuestBugFixItem _questSelecionada;
        private ScriptInstance _instanciaSelecionada;
        private PapyrusVarDisplayItem _variavelInstanciaSelecionada;
        private ArrayInfo _arraySelecionado;
        private PapyrusVarDisplayItem _elementoArraySelecionado;
        private ActiveScript _threadSelecionada;
        private TString _stringSelecionada;
        private ChangeForm _changeFormSelecionado;
        private string _hexDumpTexto = string.Empty;

        public UndoRedoManager Historico { get; } = new UndoRedoManager();

        public EssFile SaveAtual
        {
            get => _saveAtual;
            private set
            {
                if (SetProperty(ref _saveAtual, value))
                {
                    OnPropertyChanged(nameof(SaveCarregado));
                    OnPropertyChanged(nameof(PossuiPapyrus));
                }
            }
        }

        public bool SaveCarregado => _saveAtual != null;
        public bool PossuiPapyrus => _saveAtual != null && _saveAtual.Papyrus != null;
        public bool PossuiPlayerNpc { get => _possuiPlayerNpc; set => SetProperty(ref _possuiPlayerNpc, value); }

        public bool Carregando { get => _carregando; set => SetProperty(ref _carregando, value); }
        public string StatusTexto { get => _statusTexto; set => SetProperty(ref _statusTexto, value); }
        public string MemoriaTexto { get => _memoriaTexto; set => SetProperty(ref _memoriaTexto, value); }
        public string TituloJanela { get => _tituloJanela; set => SetProperty(ref _tituloJanela, value); }

        public BitmapSource CapturaTela { get => _capturaTela; set => SetProperty(ref _capturaTela, value); }
        public string NomePersonagem { get => _nomePersonagem; set => SetProperty(ref _nomePersonagem, value); }
        public string JogoNome { get => _jogoNome; set => SetProperty(ref _jogoNome, value); }
        public string NivelTexto { get => _nivelTexto; set => SetProperty(ref _nivelTexto, value); }
        public string RacaGenero { get => _racaGenero; set => SetProperty(ref _racaGenero, value); }
        public string Localizacao { get => _localizacao; set => SetProperty(ref _localizacao, value); }
        public string DataJogo { get => _dataJogo; set => SetProperty(ref _dataJogo, value); }
        public string TamanhoDiscoTexto { get => _tamanhoDiscoTexto; set => SetProperty(ref _tamanhoDiscoTexto, value); }
        public string TamanhoDescompactadoTexto { get => _tamanhoDescompactadoTexto; set => SetProperty(ref _tamanhoDescompactadoTexto, value); }
        public string CompressaoTexto { get => _compressaoTexto; set => SetProperty(ref _compressaoTexto, value); }
        public string StatusArquivoTexto { get => _statusArquivoTexto; set => SetProperty(ref _statusArquivoTexto, value); }

        public int PontuacaoSaude { get => _pontuacaoSaude; set => SetProperty(ref _pontuacaoSaude, value); }
        public string NivelSaude { get => _nivelSaude; set => SetProperty(ref _nivelSaude, value); }
        public string ResumoPapyrus { get => _resumoPapyrus; set => SetProperty(ref _resumoPapyrus, value); }
        public string ResumoChangeForms { get => _resumoChangeForms; set => SetProperty(ref _resumoChangeForms, value); }
        public string ResumoPlugins { get => _resumoPlugins; set => SetProperty(ref _resumoPlugins, value); }

        public int EditLevel { get => _editLevel; set => SetProperty(ref _editLevel, value); }
        public float EditCurrentXp { get => _editCurrentXp; set => SetProperty(ref _editCurrentXp, value); }
        public float EditHealth { get => _editHealth; set => SetProperty(ref _editHealth, value); }
        public float EditSecondaryAttr { get => _editSecondaryAttr; set => SetProperty(ref _editSecondaryAttr, value); }
        public float EditTertiaryAttr { get => _editTertiaryAttr; set => SetProperty(ref _editTertiaryAttr, value); }
        public float EditCarryWeight { get => _editCarryWeight; set => SetProperty(ref _editCarryWeight, value); }
        public float EditSpeedMult { get => _editSpeedMult; set => SetProperty(ref _editSpeedMult, value); }
        public float EditDragonSouls { get => _editDragonSouls; set => SetProperty(ref _editDragonSouls, value); }
        public int EditCurrency { get => _editCurrency; set => SetProperty(ref _editCurrency, value); }
        public int EditLockpicks { get => _editLockpicks; set => SetProperty(ref _editLockpicks, value); }

        public bool PossuiActorValues { get => _possuiActorValues; set => SetProperty(ref _possuiActorValues, value); }
        public bool PossuiCurrency { get => _possuiCurrency; set => SetProperty(ref _possuiCurrency, value); }
        public bool PossuiLockpicks { get => _possuiLockpicks; set => SetProperty(ref _possuiLockpicks, value); }
        public bool PossuiDragonSouls { get => _possuiDragonSouls; set => SetProperty(ref _possuiDragonSouls, value); }
        public bool IsSkyrimSave { get => _isSkyrimSave; set => SetProperty(ref _isSkyrimSave, value); }
        public string LabelSecondaryAttr { get => _labelSecondaryAttr; set => SetProperty(ref _labelSecondaryAttr, value); }
        public string LabelTertiaryAttr { get => _labelTertiaryAttr; set => SetProperty(ref _labelTertiaryAttr, value); }
        public string LabelCurrency { get => _labelCurrency; set => SetProperty(ref _labelCurrency, value); }
        public string LabelLockpicks { get => _labelLockpicks; set => SetProperty(ref _labelLockpicks, value); }
        public string LabelSkillsTab { get => _labelSkillsTab; set => SetProperty(ref _labelSkillsTab, value); }

        public string FiltroPlugins { get => _filtroPlugins; set { if (SetProperty(ref _filtroPlugins, value)) AtualizarListaPlugins(); } }
        public string FiltroMiscStats { get => _filtroMiscStats; set { if (SetProperty(ref _filtroMiscStats, value)) AtualizarListaMiscStats(); } }
        public string FiltroScripts { get => _filtroScripts; set { if (SetProperty(ref _filtroScripts, value)) AtualizarListaScripts(); } }
        public string FiltroInstancias { get => _filtroInstancias; set { if (SetProperty(ref _filtroInstancias, value)) AtualizarListaInstancias(); } }
        public bool SomenteInstanciasSoltas { get => _somenteInstanciasSoltas; set { if (SetProperty(ref _somenteInstanciasSoltas, value)) AtualizarListaInstancias(); } }
        public bool SomenteInstanciasIndefinidas { get => _somenteInstanciasIndefinidas; set { if (SetProperty(ref _somenteInstanciasIndefinidas, value)) AtualizarListaInstancias(); } }
        public string FiltroArrays { get => _filtroArrays; set { if (SetProperty(ref _filtroArrays, value)) AtualizarListaArrays(); } }
        public string FiltroStrings { get => _filtroStrings; set { if (SetProperty(ref _filtroStrings, value)) AtualizarListaStrings(); } }
        public string FiltroChangeForms { get => _filtroChangeForms; set { if (SetProperty(ref _filtroChangeForms, value)) AtualizarListaChangeForms(); } }
        public string FiltroGlobais { get => _filtroGlobais; set { if (SetProperty(ref _filtroGlobais, value)) AtualizarListaGlobais(); } }
        public string FiltroQuests { get => _filtroQuests; set { if (SetProperty(ref _filtroQuests, value)) AtualizarListaQuests(); } }

        public GlobalPropertyPreset GlobalPresetSelecionado { get => _globalPresetSelecionado; set => SetProperty(ref _globalPresetSelecionado, value); }
        public QuestBugFixItem QuestSelecionada { get => _questSelecionada; set => SetProperty(ref _questSelecionada, value); }
        public ScriptInstance InstanciaSelecionada { get => _instanciaSelecionada; set { if (SetProperty(ref _instanciaSelecionada, value)) AtualizarVariaveisInstancia(); } }
        public PapyrusVarDisplayItem VariavelInstanciaSelecionada { get => _variavelInstanciaSelecionada; set => SetProperty(ref _variavelInstanciaSelecionada, value); }
        public ArrayInfo ArraySelecionado { get => _arraySelecionado; set { if (SetProperty(ref _arraySelecionado, value)) AtualizarElementosArray(); } }
        public PapyrusVarDisplayItem ElementoArraySelecionado { get => _elementoArraySelecionado; set => SetProperty(ref _elementoArraySelecionado, value); }
        public ActiveScript ThreadSelecionada { get => _threadSelecionada; set => SetProperty(ref _threadSelecionada, value); }
        public TString StringSelecionada { get => _stringSelecionada; set => SetProperty(ref _stringSelecionada, value); }

        public ChangeForm ChangeFormSelecionado
        {
            get => _changeFormSelecionado;
            set
            {
                if (SetProperty(ref _changeFormSelecionado, value))
                {
                    HexDumpTexto = value != null ? value.GerarHexDump(4096) : string.Empty;
                }
            }
        }

        public string HexDumpTexto { get => _hexDumpTexto; set => SetProperty(ref _hexDumpTexto, value); }

        public ObservableCollection<string> AlertasSaude { get; } = new ObservableCollection<string>();
        public ObservableCollection<PlayerSkillItem> PlayerSkills { get; } = new ObservableCollection<PlayerSkillItem>();
        public ObservableCollection<PlayerActorValueItem> PlayerActorValues { get; } = new ObservableCollection<PlayerActorValueItem>();
        public ObservableCollection<GlobalPropertyPreset> GlobalPresets { get; } = new ObservableCollection<GlobalPropertyPreset>();
        public ObservableCollection<QuestBugFixItem> QuestFixesFiltradas { get; } = new ObservableCollection<QuestBugFixItem>();
        public ObservableCollection<PluginImpactItem> PluginsImpacto { get; } = new ObservableCollection<PluginImpactItem>();
        public ObservableCollection<MiscStatEntry> MiscStatsFiltrados { get; } = new ObservableCollection<MiscStatEntry>();
        public ObservableCollection<PapyrusScript> ScriptsFiltrados { get; } = new ObservableCollection<PapyrusScript>();
        public ObservableCollection<ScriptInstance> InstanciasFiltradas { get; } = new ObservableCollection<ScriptInstance>();
        public ObservableCollection<PapyrusVarDisplayItem> VariaveisInstancia { get; } = new ObservableCollection<PapyrusVarDisplayItem>();
        public ObservableCollection<ArrayInfo> ArraysFiltrados { get; } = new ObservableCollection<ArrayInfo>();
        public ObservableCollection<PapyrusVarDisplayItem> ElementosArray { get; } = new ObservableCollection<PapyrusVarDisplayItem>();
        public ObservableCollection<ActiveScript> ThreadsAtivas { get; } = new ObservableCollection<ActiveScript>();
        public ObservableCollection<TString> StringsFiltradas { get; } = new ObservableCollection<TString>();
        public ObservableCollection<ChangeForm> ChangeFormsFiltrados { get; } = new ObservableCollection<ChangeForm>();
        public ObservableCollection<GlobalVariable> GlobaisFiltradas { get; } = new ObservableCollection<GlobalVariable>();
        public ObservableCollection<LogEntryItem> Logs { get; } = new ObservableCollection<LogEntryItem>();

        private List<PluginImpactItem> _todosPluginsImpacto = new List<PluginImpactItem>();
        private List<QuestBugFixItem> _todasQuestsCatalogo = new List<QuestBugFixItem>();

        public MainViewModel()
        {
            AtualizarMemoria();
            _todasQuestsCatalogo = QuestFixerCatalog.GetAll();
            AtualizarListaQuests();
            foreach (GlobalPropertyPreset p in GlobalPropertiesCatalog.GetAll()) GlobalPresets.Add(p);
            RegistrarLog("INFO", "DLM.BethesdaSaveEditor (WPF Edition by xaotix) initialized.");
        }

        public void AtualizarMemoria()
        {
            long mb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
            MemoriaTexto = string.Format("{0}: {1} MB", LocalizationService.Instance["StatusMemory"], mb);
            Historico.NotifyAll();
        }

        public void RegistrarLog(string nivel, string mensagem)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logs.Insert(0, new LogEntryItem
                {
                    Hora = DateTime.Now.ToString("HH:mm:ss"),
                    Nivel = nivel,
                    Mensagem = mensagem
                });
                if (Logs.Count > 500) Logs.RemoveAt(Logs.Count - 1);
            }));
        }

        public async Task CarregarSaveAsync(string caminho)
        {
            Carregando = true;
            StatusTexto = "Loading " + Path.GetFileName(caminho) + "...";
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                EssFile ess = await Task.Run(() => EssFile.Load(caminho));
                sw.Stop();

                SaveAtual = ess;
                Historico.Clear();
                CarregarPlayerNpcECatalogos();
                AtualizarTudoNaInterface();

                StatusTexto = string.Format("Loaded in {0:N0} ms: {1}", sw.ElapsedMilliseconds, Path.GetFileName(caminho));
                RegistrarLog("OK", string.Format("Loaded ({0:N2} MB) in {1} ms: {2}",
                    ess.FileSizeOnDisk / 1048576.0, sw.ElapsedMilliseconds, caminho));

                foreach (string aviso in ess.Avisos) RegistrarLog("WARN", aviso);
            }
            catch (Exception ex)
            {
                StatusTexto = "Failed to load file.";
                RegistrarLog("ERROR", "Load failed: " + ex.Message);
                MessageBox.Show("Could not open save file:\n\n" + ex.Message,
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Carregando = false;
                AtualizarMemoria();
            }
        }

        private void CarregarPlayerNpcECatalogos()
        {
            if (_saveAtual == null) return;

            _playerNpc = new PlayerNpcEditor(_saveAtual);
            PossuiPlayerNpc = _playerNpc.IsAvailable;
            SincronizarCamposPlayerNpc();

            GlobalPresets.Clear();
            List<GlobalPropertyPreset> presets = GlobalPropertiesCatalog.GetForGame(_saveAtual.Header.Game);
            GlobalPropertiesCatalog.SyncFromSave(_saveAtual, presets);
            foreach (GlobalPropertyPreset p in presets) GlobalPresets.Add(p);

            _todasQuestsCatalogo = QuestFixerCatalog.GetCatalogForGame(_saveAtual.Header.Game);
            QuestFixerCatalog.RefreshStatus(_saveAtual, _todasQuestsCatalogo);
            AtualizarListaQuests();
        }

        private void SincronizarCamposPlayerNpc()
        {
            if (_saveAtual == null) return;
            bool isFo4 = _saveAtual.Header.Game.EhFallout4();
            IsSkyrimSave = !isFo4;
            LabelSecondaryAttr = isFo4 ? "Action Points:" : "Magicka:";
            LabelTertiaryAttr = isFo4 ? "Rads:" : "Stamina:";
            LabelCurrency = isFo4 ? "Bottlecaps:" : "Gold (Septims):";
            LabelLockpicks = isFo4 ? "Bobby Pins:" : "Lockpicks:";
            LabelSkillsTab = isFo4 ? "S.P.E.C.I.A.L." : "18 Skills & Legendary";

            EditLevel = _saveAtual.Header.Level;
            EditCurrentXp = _saveAtual.Header.CurrentXp;
            PlayerSkills.Clear();
            PlayerActorValues.Clear();

            if (_playerNpc != null && _playerNpc.IsAvailable)
            {
                PossuiActorValues = _playerNpc.HasActorValues;
                PossuiCurrency = _playerNpc.HasCurrency;
                PossuiLockpicks = _playerNpc.HasLockpicks;
                PossuiDragonSouls = _playerNpc.HasDragonSouls;

                EditHealth = _playerNpc.Health;
                EditSecondaryAttr = _playerNpc.SecondaryAttr;
                EditTertiaryAttr = _playerNpc.TertiaryAttr;
                EditCarryWeight = _playerNpc.CarryWeight;
                EditSpeedMult = _playerNpc.SpeedMult;
                EditDragonSouls = _playerNpc.DragonSouls;
                EditCurrency = _playerNpc.CurrencyCount;
                EditLockpicks = _playerNpc.LockpicksCount;

                foreach (PlayerSkillItem sk in _playerNpc.Skills) PlayerSkills.Add(sk);
                foreach (PlayerActorValueItem av in _playerNpc.ActorValues) PlayerActorValues.Add(av);
            }
            else
            {
                PossuiActorValues = false;
                PossuiCurrency = false;
                PossuiLockpicks = false;
                PossuiDragonSouls = false;
            }
        }

        public void AtualizarTudoNaInterface()
        {
            if (_saveAtual == null) return;

            SaveHeader h = _saveAtual.Header;
            string mod = _saveAtual.HasChanges ? " *" : string.Empty;
            TituloJanela = string.Format("{0}{1} - DLM.BethesdaSaveEditor (xaotix)",
                Path.GetFileName(_saveAtual.FilePath), mod);

            CapturaTela = h.CreateScreenshotBitmap();
            NomePersonagem = h.CharacterName.Text;
            JogoNome = h.Game.ObterNome();
            NivelTexto = string.Format("Level {0} (XP: {1:N0}/{2:N0})", h.Level, h.CurrentXp, h.CurrentXp + h.NeededXp);
            RacaGenero = string.Format("{0} - {1}", h.RacaLimpa, h.Sex == 0 ? LocalizationService.Instance["GenderMale"] : LocalizationService.Instance["GenderFemale"]);
            Localizacao = h.Location.Text;
            DataJogo = h.GameDate.Text;
            TamanhoDiscoTexto = string.Format("{0:N2} MB", _saveAtual.FileSizeOnDisk / 1048576.0);
            TamanhoDescompactadoTexto = string.Format("{0:N2} MB", _saveAtual.CalculateTotalUncompressedSize() / 1048576.0);
            CompressaoTexto = h.Compression.ToString();
            StatusArquivoTexto = _saveAtual.IsBroken
                ? LocalizationService.Instance["StatusBroken"]
                : (_saveAtual.HasCosave ? LocalizationService.Instance["StatusHealthyCosave"] : LocalizationService.Instance["StatusHealthy"]);

            SaveHealthReport saude = _saveAtual.AnalyzeHealth();
            PontuacaoSaude = saude.Pontuacao;
            NivelSaude = string.Format("{0}/100 - {1}", saude.Pontuacao, saude.NivelSaude);

            AlertasSaude.Clear();
            if (saude.Alertas.Count == 0) AlertasSaude.Add("No issues detected.");
            else foreach (string a in saude.Alertas) AlertasSaude.Add(a);

            if (_saveAtual.Papyrus != null)
            {
                PapyrusBlock p = _saveAtual.Papyrus;
                ResumoPapyrus = string.Format("{0:N0} scripts | {1:N0} instances | {2:N0} threads | {3:N0} strings ({4:N1}%)",
                    p.Scripts.Count, p.ScriptInstances.Count, p.ActiveScripts.Count, p.Strings.Count, saude.PercentualPapyrus);
            }
            else ResumoPapyrus = "Papyrus unavailable";

            ResumoChangeForms = string.Format("{0:N0} records ({1:N1}%)", _saveAtual.ChangeForms.Count, saude.PercentualChangeForms);
            ResumoPlugins = string.Format("{0} full + {1} light (ESL)",
                _saveAtual.Plugins.FullPlugins.Count,
                _saveAtual.Plugins.LitePlugins != null ? _saveAtual.Plugins.LitePlugins.Count : 0);

            GlobalPropertiesCatalog.SyncFromSave(_saveAtual, GlobalPresets);
            QuestFixerCatalog.RefreshStatus(_saveAtual, _todasQuestsCatalogo);

            CalcularImpactoPlugins();
            AtualizarListaPlugins();
            AtualizarListaMiscStats();
            AtualizarListaScripts();
            AtualizarListaInstancias();
            AtualizarListaArrays();
            AtualizarListaThreads();
            AtualizarListaStrings();
            AtualizarListaChangeForms();
            AtualizarListaGlobais();
            AtualizarListaQuests();
        }

        private void CalcularImpactoPlugins()
        {
            _todosPluginsImpacto.Clear();
            if (_saveAtual == null) return;

            Dictionary<string, int> cfCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, long> cfBytes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> instCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            long totalCfBytes = 0;
            foreach (ChangeForm cf in _saveAtual.ChangeForms)
            {
                string pName = cf.PluginNome;
                int size = cf.CalculateSize();
                totalCfBytes += size;
                int c; cfCount[pName] = cfCount.TryGetValue(pName, out c) ? c + 1 : 1;
                long b; cfBytes[pName] = cfBytes.TryGetValue(pName, out b) ? b + size : size;
            }

            if (_saveAtual.Papyrus != null)
            {
                foreach (ScriptInstance inst in _saveAtual.Papyrus.ScriptInstances.Values)
                {
                    string pName = inst.PluginNome;
                    int c; instCount[pName] = instCount.TryGetValue(pName, out c) ? c + 1 : 1;
                }
            }

            double denom = Math.Max(1.0, totalCfBytes);
            foreach (Plugin p in _saveAtual.Plugins.AllPlugins())
            {
                int cfc; long cfb; int ic;
                cfCount.TryGetValue(p.Name, out cfc);
                cfBytes.TryGetValue(p.Name, out cfb);
                instCount.TryGetValue(p.Name, out ic);

                _todosPluginsImpacto.Add(new PluginImpactItem
                {
                    Indice = p.IndexDisplay,
                    Nome = p.Name,
                    Tipo = p.IsLightweight ? "ESL" : "ESP/ESM",
                    ChangeForms = cfc,
                    InstanciasScript = ic,
                    BytesChangeForms = cfb,
                    PercentualTamanho = (cfb / denom) * 100.0
                });
            }
        }

        private void AtualizarListaPlugins()
        {
            PluginsImpacto.Clear();
            string q = (_filtroPlugins ?? string.Empty).Trim();
            IEnumerable<PluginImpactItem> query = _todosPluginsImpacto;
            if (q.Length > 0) query = query.Where(p => p.Nome.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || p.Indice.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (PluginImpactItem item in query.OrderByDescending(p => p.BytesChangeForms)) PluginsImpacto.Add(item);
        }

        private void AtualizarListaMiscStats()
        {
            MiscStatsFiltrados.Clear();
            if (_saveAtual == null || _saveAtual.MiscStats == null) return;
            string q = (_filtroMiscStats ?? string.Empty).Trim();
            IEnumerable<MiscStatEntry> query = _saveAtual.MiscStats.Entries;
            if (q.Length > 0) query = query.Where(m => m.NameText.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || m.CategoryName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (MiscStatEntry e in query.OrderBy(m => m.Category).ThenBy(m => m.NameText)) MiscStatsFiltrados.Add(e);
        }

        private void AtualizarListaScripts()
        {
            ScriptsFiltrados.Clear();
            if (_saveAtual == null || _saveAtual.Papyrus == null) return;
            string q = (_filtroScripts ?? string.Empty).Trim();
            IEnumerable<PapyrusScript> query = _saveAtual.Papyrus.Scripts.Values;
            if (q.Length > 0) query = query.Where(s => s.NomeTexto.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || s.PaiTexto.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (PapyrusScript s in query.OrderByDescending(s => s.InstanceCount).Take(2000)) ScriptsFiltrados.Add(s);
        }

        private void AtualizarListaInstancias()
        {
            InstanciasFiltradas.Clear();
            if (_saveAtual == null || _saveAtual.Papyrus == null) return;
            string q = (_filtroInstancias ?? string.Empty).Trim();
            IEnumerable<ScriptInstance> query = _saveAtual.Papyrus.ScriptInstances.Values;
            if (_somenteInstanciasSoltas) query = query.Where(i => i.IsUnattached);
            if (_somenteInstanciasIndefinidas) query = query.Where(i => i.IsUndefined);
            if (q.Length > 0) query = query.Where(i => i.ScriptNome.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || i.RefIDHex.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || i.IdHex.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (ScriptInstance inst in query.Take(3000)) InstanciasFiltradas.Add(inst);
        }

        public void AtualizarVariaveisInstancia()
        {
            VariaveisInstancia.Clear();
            if (_instanciaSelecionada == null || _instanciaSelecionada.Variables == null) return;
            List<MemberDesc> members = _instanciaSelecionada.ScriptDef != null ? _instanciaSelecionada.ScriptDef.Members : null;
            for (int i = 0; i < _instanciaSelecionada.Variables.Count; i++)
            {
                PapyrusVariable v = _instanciaSelecionada.Variables[i];
                string nome = (members != null && i < members.Count) ? members[i].Name.Text : ("Var #" + i);
                VariaveisInstancia.Add(new PapyrusVarDisplayItem { Indice = i, NomeMembro = nome, Tipo = v.VarType.ToString(), Valor = v.ToValueDisplay(), Editavel = v.IsEditable, VariavelRef = v });
            }
        }

        private void AtualizarListaArrays()
        {
            ArraysFiltrados.Clear();
            if (_saveAtual == null || _saveAtual.Papyrus == null) return;
            string q = (_filtroArrays ?? string.Empty).Trim();
            IEnumerable<ArrayInfo> query = _saveAtual.Papyrus.Arrays.Values;
            if (q.Length > 0) query = query.Where(a => a.IdHex.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || a.TipoTexto.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (ArrayInfo a in query.Take(2000)) ArraysFiltrados.Add(a);
        }

        public void AtualizarElementosArray()
        {
            ElementosArray.Clear();
            if (_arraySelecionado == null || _arraySelecionado.Variables == null) return;
            for (int i = 0; i < _arraySelecionado.Variables.Count; i++)
            {
                PapyrusVariable v = _arraySelecionado.Variables[i];
                ElementosArray.Add(new PapyrusVarDisplayItem { Indice = i, NomeMembro = "[" + i + "]", Tipo = v.VarType.ToString(), Valor = v.ToValueDisplay(), Editavel = v.IsEditable, VariavelRef = v });
            }
        }

        private void AtualizarListaThreads()
        {
            ThreadsAtivas.Clear();
            if (_saveAtual == null || _saveAtual.Papyrus == null) return;
            foreach (ActiveScript a in _saveAtual.Papyrus.ActiveScripts.Values) ThreadsAtivas.Add(a);
        }

        public void AtualizarListaStrings()
        {
            StringsFiltradas.Clear();
            if (_saveAtual == null || _saveAtual.Papyrus == null) return;
            string q = (_filtroStrings ?? string.Empty).Trim();
            IEnumerable<TString> query = _saveAtual.Papyrus.Strings.Strings;
            if (q.Length > 0) query = query.Where(s => s.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (TString s in query.Take(3000)) StringsFiltradas.Add(s);
        }

        public void AtualizarListaChangeForms()
        {
            ChangeFormsFiltrados.Clear();
            if (_saveAtual == null) return;
            string q = (_filtroChangeForms ?? string.Empty).Trim();
            IEnumerable<ChangeForm> query = _saveAtual.ChangeForms;
            if (q.Length > 0) query = query.Where(c => c.FormIDHex.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || c.TipoCodigo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || c.PluginNome.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (ChangeForm cf in query.Take(3000)) ChangeFormsFiltrados.Add(cf);
        }

        private void AtualizarListaGlobais()
        {
            GlobaisFiltradas.Clear();
            if (_saveAtual == null || _saveAtual.GlobalVariables == null) return;
            string q = (_filtroGlobais ?? string.Empty).Trim();
            IEnumerable<GlobalVariable> query = _saveAtual.GlobalVariables.Variables;
            if (q.Length > 0) query = query.Where(g => g.FormIDHex.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || g.PluginNome.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || g.NomeConhecido.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (GlobalVariable gv in query.Take(2000)) GlobaisFiltradas.Add(gv);
        }

        private void AtualizarListaQuests()
        {
            QuestFixesFiltradas.Clear();
            string q = (_filtroQuests ?? string.Empty).Trim();
            IEnumerable<QuestBugFixItem> query = _todasQuestsCatalogo;
            if (q.Length > 0) query = query.Where(x => x.QuestName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || x.EditorID.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || x.BugDescription.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (QuestBugFixItem item in query) QuestFixesFiltradas.Add(item);
        }

        // === UNDO / REDO ===

        public void ExecutarUndo()
        {
            string desc = Historico.PerformUndo();
            if (desc != null)
            {
                SincronizarCamposPlayerNpc();
                AtualizarTudoNaInterface();
                AtualizarVariaveisInstancia();
                AtualizarElementosArray();
                StatusTexto = "Undo: " + desc;
                RegistrarLog("UNDO", desc);
            }
        }

        public void ExecutarRedo()
        {
            string desc = Historico.PerformRedo();
            if (desc != null)
            {
                SincronizarCamposPlayerNpc();
                AtualizarTudoNaInterface();
                AtualizarVariaveisInstancia();
                AtualizarElementosArray();
                StatusTexto = "Redo: " + desc;
                RegistrarLog("REDO", desc);
            }
        }

        // === CHARACTER & GLOBAL PROPERTIES ===

        public void AplicarAlteracoesPersonagem()
        {
            if (_saveAtual == null) return;

            int newLevel = Math.Max(1, EditLevel);
            float newXp = Math.Max(0f, EditCurrentXp);
            PlayerSnapshot oldSnap = _playerNpc != null ? _playerNpc.CaptureSnapshot(_saveAtual) : null;

            if (_playerNpc != null && _playerNpc.IsAvailable)
            {
                _playerNpc.Health = Math.Max(1f, EditHealth);
                _playerNpc.SecondaryAttr = Math.Max(0f, EditSecondaryAttr);
                _playerNpc.TertiaryAttr = Math.Max(0f, EditTertiaryAttr);
                _playerNpc.CarryWeight = Math.Max(1f, EditCarryWeight);
                _playerNpc.SpeedMult = Math.Max(1f, EditSpeedMult);
                _playerNpc.DragonSouls = Math.Max(0f, EditDragonSouls);
                _playerNpc.CurrencyCount = Math.Max(0, EditCurrency);
                _playerNpc.LockpicksCount = Math.Max(0, EditLockpicks);
                _playerNpc.ApplyToSave(_saveAtual, newLevel, newXp);
            }
            else
            {
                _saveAtual.Header.Level = newLevel;
                _saveAtual.Header.CurrentXp = newXp;
                _saveAtual.HasChanges = true;
            }

            PlayerSnapshot newSnap = _playerNpc != null ? _playerNpc.CaptureSnapshot(_saveAtual) : null;

            if (oldSnap != null && newSnap != null)
            {
                Historico.Record(
                    "Edit Character Attributes / Skills / Inventory",
                    () => { _playerNpc.RestoreSnapshot(oldSnap, _saveAtual); },
                    () => { _playerNpc.RestoreSnapshot(newSnap, _saveAtual); });
            }

            SincronizarCamposPlayerNpc();
            AtualizarTudoNaInterface();
            StatusTexto = "Character attributes, skills & inventory updated directly in save.";
            RegistrarLog("EDIT", string.Format("Player ACHR 0x14 updated: Lv={0}, HP={1}, {2}={3}, {4}={5}, CW={6}, Currency={7}",
                newLevel, EditHealth, LabelSecondaryAttr.TrimEnd(':'), EditSecondaryAttr, LabelTertiaryAttr.TrimEnd(':'), EditTertiaryAttr, EditCarryWeight, EditCurrency));
            MessageBox.Show("Character attributes, skills & inventory written directly to save binary (ACHR 00000014).",
                "Character Editor", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void AplicarGlobalPresetSelecionado()
        {
            if (_saveAtual == null || _globalPresetSelecionado == null) return;
            GlobalVariable gv = _saveAtual.GlobalVariables.Variables.FirstOrDefault(g => g.RefID.FormID == _globalPresetSelecionado.FormID);
            if (gv == null)
            {
                MessageBox.Show("This GlobalVariable is not present in the current save's GlobalVariableTable.", "Global Property", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            float oldVal = gv.Valor;
            float newVal = _globalPresetSelecionado.CurrentValue;
            string name = _globalPresetSelecionado.Name;

            gv.Valor = newVal;
            _saveAtual.HasChanges = true;

            Historico.Record(
                string.Format("Global {0} = {1}", name, newVal),
                () => { gv.Valor = oldVal; },
                () => { gv.Valor = newVal; });

            AtualizarTudoNaInterface();
            StatusTexto = string.Format("Updated {0} = {1}", name, newVal);
            RegistrarLog("EDIT", StatusTexto);
        }

        // === QUEST BUG FIXER ===

        public void AplicarCorrecaoQuestSelecionada()
        {
            if (_saveAtual == null || _questSelecionada == null) return;
            ChangeForm targetForm;
            byte[] prevBody;
            string msg;

            if (QuestFixerCatalog.ApplyBinaryStageFix(_saveAtual, _questSelecionada, out targetForm, out prevBody, out msg))
            {
                byte[] newBody = targetForm.GetDecompressedBody();
                string desc = "Quest Fix: " + _questSelecionada.EditorID;
                Historico.Record(
                    desc,
                    () => { targetForm.UpdateBodyData(prevBody); },
                    () => { targetForm.UpdateBodyData(newBody); });

                AtualizarTudoNaInterface();
                StatusTexto = desc;
                RegistrarLog("QUEST", msg);
                MessageBox.Show(msg, "Quest Bug Fixer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(msg, "Quest Bug Fixer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // === CLEANING & EDITING WITH UNDO/REDO ===

        public void ExecutarLimpezaUnattached()
        {
            if (_saveAtual == null || _saveAtual.Papyrus == null) return;
            List<ScriptInstance> removed = _saveAtual.Papyrus.ScriptInstances.Values.Where(i => i.IsUnattached).ToList();
            if (removed.Count == 0) { MessageBox.Show("No unattached instances found.", "Clean", MessageBoxButton.OK, MessageBoxImage.Information); return; }

            foreach (ScriptInstance s in removed) _saveAtual.Papyrus.ScriptInstances.Remove(s.Id);
            _saveAtual.HasChanges = true;

            Historico.Record(
                string.Format("Remove {0} Unattached", removed.Count),
                () => { foreach (ScriptInstance s in removed) _saveAtual.Papyrus.ScriptInstances[s.Id] = s; },
                () => { foreach (ScriptInstance s in removed) _saveAtual.Papyrus.ScriptInstances.Remove(s.Id); });

            AtualizarTudoNaInterface();
            StatusTexto = string.Format("Removed {0} unattached instance(s).", removed.Count);
            RegistrarLog("CLEAN", StatusTexto);
            MessageBox.Show(StatusTexto, "Clean Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExecutarLimpezaUndefined()
        {
            if (_saveAtual == null) return;
            int n = _saveAtual.RemoveUndefinedElements();
            AtualizarTudoNaInterface();
            StatusTexto = string.Format("Removed/terminated {0} undefined element(s).", n);
            RegistrarLog("CLEAN", StatusTexto);
            MessageBox.Show(StatusTexto, "Clean Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExecutarLimpezaNonExistent()
        {
            if (_saveAtual == null || _saveAtual.Papyrus == null) return;
            HashSet<RefID> existing = new HashSet<RefID>(_saveAtual.ChangeForms.Select(cf => cf.RefID));
            List<ScriptInstance> removed = _saveAtual.Papyrus.ScriptInstances.Values
                .Where(i => i.RefID.Type == RefIDType.Created && !existing.Contains(i.RefID)).ToList();
            if (removed.Count == 0) { MessageBox.Show("No non-existent created instances found.", "Clean", MessageBoxButton.OK, MessageBoxImage.Information); return; }

            foreach (ScriptInstance s in removed) _saveAtual.Papyrus.ScriptInstances.Remove(s.Id);
            _saveAtual.HasChanges = true;

            Historico.Record(
                string.Format("Remove {0} Non-Existent", removed.Count),
                () => { foreach (ScriptInstance s in removed) _saveAtual.Papyrus.ScriptInstances[s.Id] = s; },
                () => { foreach (ScriptInstance s in removed) _saveAtual.Papyrus.ScriptInstances.Remove(s.Id); });

            AtualizarTudoNaInterface();
            StatusTexto = string.Format("Removed {0} non-existent created form instance(s).", removed.Count);
            RegistrarLog("CLEAN", StatusTexto);
            MessageBox.Show(StatusTexto, "Clean Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExecutarPurificarFormLists()
        {
            if (_saveAtual == null) return;
            int[] res = _saveAtual.PurifyFormLists();
            AtualizarTudoNaInterface();
            StatusTexto = string.Format("Removed {0} null ref(s) across {1} FormList(s).", res[0], res[1]);
            RegistrarLog("CLEAN", StatusTexto);
            MessageBox.Show(StatusTexto, "Purify FormLists", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExecutarLimpezaCompleta()
        {
            if (_saveAtual == null) return;
            int u1 = _saveAtual.RemoveUnattachedInstances();
            int u2 = _saveAtual.RemoveUndefinedElements();
            int u3 = _saveAtual.RemoveNonExistentCreatedInstances();
            int[] fl = _saveAtual.PurifyFormLists();
            AtualizarTudoNaInterface();

            string msg = string.Format(
                "Full clean completed:\n- Unattached instances removed: {0}\n- Undefined elements/threads handled: {1}\n- Non-existent created instances removed: {2}\n- FormList null refs removed: {3} ({4} lists)",
                u1, u2, u3, fl[0], fl[1]);
            StatusTexto = string.Format("Full clean: {0} item(s) processed.", u1 + u2 + u3 + fl[0]);
            RegistrarLog("CLEAN", StatusTexto);
            MessageBox.Show(msg, "Full Clean", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ZerarTodasRecompensas()
        {
            if (_saveAtual == null || _saveAtual.MiscStats == null) return;
            List<Tuple<MiscStatEntry, int>> prev = _saveAtual.MiscStats.Entries
                .Where(e => e.Category == 5 && e.NameText.IndexOf("Bounty", StringComparison.OrdinalIgnoreCase) >= 0 && e.Value > 0)
                .Select(e => Tuple.Create(e, e.Value))
                .ToList();

            if (prev.Count == 0)
            {
                MessageBox.Show("All bounties are already 0.", "Misc Stats", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (Tuple<MiscStatEntry, int> t in prev) t.Item1.Value = 0;
            _saveAtual.HasChanges = true;

            Historico.Record(
                "Clear All Bounties",
                () => { foreach (Tuple<MiscStatEntry, int> t in prev) t.Item1.Value = t.Item2; },
                () => { foreach (Tuple<MiscStatEntry, int> t in prev) t.Item1.Value = 0; });

            AtualizarTudoNaInterface();
            StatusTexto = string.Format("Cleared {0} active bounty stat(s).", prev.Count);
            RegistrarLog("EDIT", StatusTexto);
        }

        public void ExcluirInstanciaSelecionada()
        {
            if (_saveAtual == null || _saveAtual.Papyrus == null || _instanciaSelecionada == null) return;
            ScriptInstance inst = _instanciaSelecionada;
            if (_saveAtual.Papyrus.ScriptInstances.Remove(inst.Id))
            {
                _saveAtual.HasChanges = true;
                InstanciaSelecionada = null;
                Historico.Record(
                    "Delete Instance " + inst.ScriptNome,
                    () => { _saveAtual.Papyrus.ScriptInstances[inst.Id] = inst; },
                    () => { _saveAtual.Papyrus.ScriptInstances.Remove(inst.Id); });
                AtualizarTudoNaInterface();
                RegistrarLog("EDIT", string.Format("Deleted ScriptInstance: {0} ({1})", inst.ScriptNome, inst.IdHex));
            }
        }

        public void EncerrarThreadSelecionada()
        {
            if (_saveAtual == null || _threadSelecionada == null) return;
            _threadSelecionada.Zero();
            _saveAtual.HasChanges = true;
            AtualizarTudoNaInterface();
            RegistrarLog("EDIT", "Terminated (zeroed) ActiveScript thread: " + _threadSelecionada.IdHex);
        }

        public void ExcluirThreadSelecionada()
        {
            if (_saveAtual == null || _saveAtual.Papyrus == null || _threadSelecionada == null) return;
            ActiveScript act = _threadSelecionada;
            if (_saveAtual.Papyrus.ActiveScripts.Remove(act.Id))
            {
                _saveAtual.HasChanges = true;
                ThreadSelecionada = null;
                Historico.Record(
                    "Delete Thread " + act.IdHex,
                    () => { _saveAtual.Papyrus.ActiveScripts[act.Id] = act; },
                    () => { _saveAtual.Papyrus.ActiveScripts.Remove(act.Id); });
                AtualizarTudoNaInterface();
                RegistrarLog("EDIT", "Deleted ActiveScript thread: " + act.IdHex);
            }
        }

        public void ExcluirChangeFormSelecionado()
        {
            if (_saveAtual == null || _changeFormSelecionado == null) return;
            ChangeForm cf = _changeFormSelecionado;
            int idx = _saveAtual.ChangeForms.IndexOf(cf);
            if (idx >= 0)
            {
                _saveAtual.ChangeForms.RemoveAt(idx);
                _saveAtual.HasChanges = true;
                ChangeFormSelecionado = null;
                Historico.Record(
                    "Delete ChangeForm " + cf.FormIDHex,
                    () => { _saveAtual.ChangeForms.Insert(Math.Min(idx, _saveAtual.ChangeForms.Count), cf); },
                    () => { _saveAtual.ChangeForms.Remove(cf); });
                AtualizarTudoNaInterface();
                RegistrarLog("EDIT", "Deleted ChangeForm: " + cf.FormIDHex);
            }
        }

        public void ExportarListaPlugins(string destino)
        {
            if (_saveAtual == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Plugin List - " + Path.GetFileName(_saveAtual.FilePath));
            sb.AppendLine("# Generated by DLM.BethesdaSaveEditor (xaotix)");
            sb.AppendLine();
            foreach (Plugin p in _saveAtual.Plugins.AllPlugins())
            {
                sb.AppendLine(string.Format("{0}\t{1}", p.IndexDisplay, p.Name));
            }
            File.WriteAllText(destino, sb.ToString(), new UTF8Encoding(true));
            RegistrarLog("EXPORT", "Plugin list exported to: " + destino);
        }
    }
}
