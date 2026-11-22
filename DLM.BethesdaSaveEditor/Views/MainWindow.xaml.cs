using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using DLM.BethesdaSaveEditor.Core;
using DLM.BethesdaSaveEditor.Ess;
using DLM.BethesdaSaveEditor.Ess.Papyrus;
using DLM.BethesdaSaveEditor.ViewModels;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly DispatcherTimer _timerMemoria;
        private int _pendingMiscOldValue;
        private float _pendingGlobalOldValue;

        public MainWindow()
            : this(null)
        {
        }

        public MainWindow(string initialSavePath)
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            _timerMemoria = new DispatcherTimer();
            _timerMemoria.Interval = TimeSpan.FromSeconds(4);
            _timerMemoria.Tick += (s, e) => _vm.AtualizarMemoria();
            _timerMemoria.Start();

            PreviewKeyDown += MainWindow_PreviewKeyDown;

            if (!string.IsNullOrWhiteSpace(initialSavePath) && File.Exists(initialSavePath))
            {
                Loaded += async (s, e) => await _vm.CarregarSaveAsync(initialSavePath);
            }
        }

        private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.O) { e.Handled = true; AbrirArquivoDialogo(); }
                else if (e.Key == Key.S && _vm.SaveCarregado) { e.Handled = true; SalvarArquivo(false); }
                else if (e.Key == Key.B) { e.Handled = true; await AbrirNavegadorSavesAsync(); }
                else if (e.Key == Key.Z && !(Keyboard.FocusedElement is TextBox) && _vm.Historico.CanUndo)
                {
                    e.Handled = true;
                    _vm.ExecutarUndo();
                }
                else if (e.Key == Key.Y && !(Keyboard.FocusedElement is TextBox) && _vm.Historico.CanRedo)
                {
                    e.Handled = true;
                    _vm.ExecutarRedo();
                }
            }
        }

        private async void AbrirArquivoDialogo()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Save File";
            dlg.Filter = "Bethesda Saves (*.ess;*.fos;*.sav0)|*.ess;*.fos;*.sav0|All Files (*.*)|*.*";

            if (dlg.ShowDialog(this) == true)
            {
                await _vm.CarregarSaveAsync(dlg.FileName);
            }
        }

        private void SalvarArquivo(bool salvarComo)
        {
            if (!_vm.SaveCarregado) return;

            string destino = _vm.SaveAtual.FilePath;
            if (salvarComo || string.IsNullOrWhiteSpace(destino))
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Title = "Save File";
                dlg.FileName = Path.GetFileName(destino);
                dlg.Filter = "Skyrim Save (*.ess)|*.ess|Fallout 4 Save (*.fos)|*.fos|Switch Save (*.sav0)|*.sav0";
                if (dlg.ShowDialog(this) != true) return;
                destino = dlg.FileName;
            }

            try
            {
                string backup = _vm.SaveAtual.Save(destino, LocalizationService.Instance.AutoBackup);
                _vm.AtualizarTudoNaInterface();
                string msg = string.IsNullOrEmpty(backup)
                    ? "Save written successfully: " + destino
                    : "Save written successfully.\nBackup created at:\n" + backup;
                _vm.StatusTexto = "Saved: " + Path.GetFileName(destino);
                _vm.RegistrarLog("SAVED", destino);
                MessageBox.Show(msg, "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _vm.RegistrarLog("ERROR", "Save failed: " + ex.Message);
                MessageBox.Show("Failed to save file:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task AbrirNavegadorSavesAsync()
        {
            JanelaNavegadorSaves win = new JanelaNavegadorSaves();
            win.Owner = this;
            if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(win.CaminhoSelecionado))
            {
                await _vm.CarregarSaveAsync(win.CaminhoSelecionado);
            }
        }

        // === MENU HANDLERS ===

        private void MenuAbrir_Click(object sender, RoutedEventArgs e) => AbrirArquivoDialogo();
        private async void MenuNavegadorSaves_Click(object sender, RoutedEventArgs e) => await AbrirNavegadorSavesAsync();
        private void MenuSalvar_Click(object sender, RoutedEventArgs e) => SalvarArquivo(false);
        private void MenuSalvarComo_Click(object sender, RoutedEventArgs e) => SalvarArquivo(true);
        private void MenuSair_Click(object sender, RoutedEventArgs e) => Close();

        private void MenuUndo_Click(object sender, RoutedEventArgs e) => _vm.ExecutarUndo();
        private void MenuRedo_Click(object sender, RoutedEventArgs e) => _vm.ExecutarRedo();

        private void MenuExportarPlugins_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.SaveCarregado) return;
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = Path.GetFileNameWithoutExtension(_vm.SaveAtual.FilePath) + "_plugins.txt";
            dlg.Filter = "Text File (*.txt)|*.txt";
            if (dlg.ShowDialog(this) == true)
            {
                _vm.ExportarListaPlugins(dlg.FileName);
                MessageBox.Show("Plugin list exported.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuExportarScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.SaveCarregado) return;
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = Path.GetFileNameWithoutExtension(_vm.SaveAtual.FilePath) + ".png";
            dlg.Filter = "PNG Image (*.png)|*.png";
            if (dlg.ShowDialog(this) == true)
            {
                try
                {
                    _vm.SaveAtual.Header.ExportScreenshotPng(dlg.FileName);
                    _vm.RegistrarLog("EXPORT", "Screenshot exported: " + dlg.FileName);
                    MessageBox.Show("Screenshot exported.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void MenuLimpezaCompleta_Click(object sender, RoutedEventArgs e) => _vm.ExecutarLimpezaCompleta();
        private void MenuLimparUnattached_Click(object sender, RoutedEventArgs e) => _vm.ExecutarLimpezaUnattached();
        private void MenuLimparUndefined_Click(object sender, RoutedEventArgs e) => _vm.ExecutarLimpezaUndefined();
        private void MenuLimparNonExistent_Click(object sender, RoutedEventArgs e) => _vm.ExecutarLimpezaNonExistent();
        private void MenuPurificarFormLists_Click(object sender, RoutedEventArgs e) => _vm.ExecutarPurificarFormLists();

        private void MenuLimpezaLote_Click(object sender, RoutedEventArgs e)
        {
            JanelaLimpezaLote win = new JanelaLimpezaLote();
            win.Owner = this;
            win.ShowDialog();
        }

        private void MenuEditarCabecalho_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.SaveCarregado) return;
            SaveHeader h = _vm.SaveAtual.Header;
            PlayerLocationBlock pl = _vm.SaveAtual.PlayerLocation;

            string oldName = h.CharacterName.Text;
            int oldLevel = h.Level;
            int oldSaveNum = h.SaveNumber;
            string oldLoc = h.Location.Text;
            string oldRace = h.RaceId.Text;
            short oldSex = h.Sex;
            float oldXp = h.CurrentXp;
            float oldNeed = h.NeededXp;
            CompressionType oldComp = h.Compression;
            float oldX = pl != null ? pl.PosX : 0f;
            float oldY = pl != null ? pl.PosY : 0f;
            float oldZ = pl != null ? pl.PosZ : 0f;
            int oldCx = pl != null ? pl.CellX : 0;
            int oldCy = pl != null ? pl.CellY : 0;

            JanelaEditarCabecalho win = new JanelaEditarCabecalho(_vm.SaveAtual);
            win.Owner = this;
            if (win.ShowDialog() == true)
            {
                string newName = h.CharacterName.Text;
                int newLevel = h.Level;
                int newSaveNum = h.SaveNumber;
                string newLoc = h.Location.Text;
                string newRace = h.RaceId.Text;
                short newSex = h.Sex;
                float newXp = h.CurrentXp;
                float newNeed = h.NeededXp;
                CompressionType newComp = h.Compression;
                float newX = pl != null ? pl.PosX : 0f;
                float newY = pl != null ? pl.PosY : 0f;
                float newZ = pl != null ? pl.PosZ : 0f;
                int newCx = pl != null ? pl.CellX : 0;
                int newCy = pl != null ? pl.CellY : 0;

                _vm.SaveAtual.HasChanges = true;
                _vm.Historico.Record(
                    "Edit Header/Coords",
                    () =>
                    {
                        h.CharacterName.Text = oldName; h.Level = oldLevel; h.SaveNumber = oldSaveNum;
                        h.Location.Text = oldLoc; h.RaceId.Text = oldRace; h.Sex = oldSex;
                        h.CurrentXp = oldXp; h.NeededXp = oldNeed; h.Compression = oldComp;
                        if (pl != null) { pl.PosX = oldX; pl.PosY = oldY; pl.PosZ = oldZ; pl.CellX = oldCx; pl.CellY = oldCy; }
                    },
                    () =>
                    {
                        h.CharacterName.Text = newName; h.Level = newLevel; h.SaveNumber = newSaveNum;
                        h.Location.Text = newLoc; h.RaceId.Text = newRace; h.Sex = newSex;
                        h.CurrentXp = newXp; h.NeededXp = newNeed; h.Compression = newComp;
                        if (pl != null) { pl.PosX = newX; pl.PosY = newY; pl.PosZ = newZ; pl.CellX = newCx; pl.CellY = newCy; }
                    });

                _vm.AtualizarTudoNaInterface();
                _vm.RegistrarLog("EDIT", "Header / Player coordinates updated.");
            }
        }

        private void MenuCompararSaves_Click(object sender, RoutedEventArgs e)
        {
            JanelaCompararSaves win = new JanelaCompararSaves(_vm.SaveAtual != null ? _vm.SaveAtual.FilePath : null);
            win.Owner = this;
            win.ShowDialog();
        }

        private void MenuOpcoes_Click(object sender, RoutedEventArgs e)
        {
            JanelaOpcoes win = new JanelaOpcoes();
            win.Owner = this;
            win.ShowDialog();
            _vm.AtualizarMemoria();
            if (_vm.SaveCarregado) _vm.AtualizarTudoNaInterface();
        }

        private void MenuSobre_Click(object sender, RoutedEventArgs e)
        {
            JanelaSobre win = new JanelaSobre();
            win.Owner = this;
            win.ShowDialog();
        }

        // === CHARACTER, GLOBALS & QUEST FIXER ===

        private void BtnAplicarPersonagem_Click(object sender, RoutedEventArgs e) => _vm.AplicarAlteracoesPersonagem();
        private void BtnAplicarGlobalPreset_Click(object sender, RoutedEventArgs e) => _vm.AplicarGlobalPresetSelecionado();

        private void BtnCorrigirQuest_Click(object sender, RoutedEventArgs e) => _vm.AplicarCorrecaoQuestSelecionada();

        private void BtnCopiarComandoQuest_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.QuestSelecionada == null) return;
            Clipboard.SetText(_vm.QuestSelecionada.ConsoleCommand);
            _vm.StatusTexto = "Copied: " + _vm.QuestSelecionada.ConsoleCommand;
        }

        // === MISC STATS & GLOBALS INLINE EDIT WITH UNDO/REDO ===

        private void BtnZerarBounties_Click(object sender, RoutedEventArgs e) => _vm.ZerarTodasRecompensas();

        private void DgMiscStats_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            MiscStatEntry entry = e.Row.Item as MiscStatEntry;
            if (entry != null) _pendingMiscOldValue = entry.Value;
        }

        private void DgMiscStats_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!_vm.SaveCarregado || e.EditAction != DataGridEditAction.Commit) return;
            MiscStatEntry entry = e.Row.Item as MiscStatEntry;
            if (entry == null) return;
            int oldVal = _pendingMiscOldValue;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                int newVal = entry.Value;
                if (newVal != oldVal)
                {
                    _vm.SaveAtual.HasChanges = true;
                    _vm.Historico.Record(
                        string.Format("Stat {0}: {1} -> {2}", entry.NameText, oldVal, newVal),
                        () => { entry.Value = oldVal; },
                        () => { entry.Value = newVal; });
                    _vm.RegistrarLog("EDIT", string.Format("MiscStat {0} = {1}", entry.NameText, newVal));
                }
            }));
        }

        private void DgGlobais_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            GlobalVariable gv = e.Row.Item as GlobalVariable;
            if (gv != null) _pendingGlobalOldValue = gv.Valor;
        }

        private void DgGlobais_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!_vm.SaveCarregado || e.EditAction != DataGridEditAction.Commit) return;
            GlobalVariable gv = e.Row.Item as GlobalVariable;
            if (gv == null) return;
            float oldVal = _pendingGlobalOldValue;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                float newVal = gv.Valor;
                if (Math.Abs(newVal - oldVal) > 1e-6f)
                {
                    _vm.SaveAtual.HasChanges = true;
                    _vm.Historico.Record(
                        string.Format("Global {0} = {1}", gv.FormIDHex, newVal),
                        () => { gv.Valor = oldVal; },
                        () => { gv.Valor = newVal; });
                    _vm.RegistrarLog("EDIT", string.Format("GlobalVariable {0} = {1}", gv.FormIDHex, newVal));
                }
            }));
        }

        // === PAPYRUS & CHANGEFORMS EDITING WITH UNDO/REDO ===

        private void BtnExcluirInstancia_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.InstanciaSelecionada == null) return;
            if (MessageBox.Show("Delete selected ScriptInstance?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _vm.ExcluirInstanciaSelecionada();
            }
        }

        private void EditarVariavel(PapyrusVarDisplayItem item, Action refreshAction)
        {
            if (item == null || item.VariavelRef == null || !_vm.PossuiPapyrus) return;
            if (!item.Editavel)
            {
                MessageBox.Show("This variable type (" + item.Tipo + ") cannot be edited directly.", "Edit Variable", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string oldText = item.VariavelRef.ToRawEditText();
            JanelaInputValor dlg = new JanelaInputValor("Edit Papyrus Variable", string.Format("{0} ({1}):", item.NomeMembro, item.Tipo), oldText);
            dlg.Owner = this;

            if (dlg.ShowDialog() == true)
            {
                string newText = dlg.ValorTexto;
                string err;
                if (item.VariavelRef.TrySetFromText(newText, _vm.SaveAtual.Papyrus, out err))
                {
                    PapyrusVariable vRef = item.VariavelRef;
                    _vm.SaveAtual.HasChanges = true;
                    _vm.Historico.Record(
                        "Edit Var " + item.NomeMembro,
                        () => { string dummy; vRef.TrySetFromText(oldText, _vm.SaveAtual.Papyrus, out dummy); },
                        () => { string dummy; vRef.TrySetFromText(newText, _vm.SaveAtual.Papyrus, out dummy); });
                    refreshAction();
                    _vm.RegistrarLog("EDIT", string.Format("Updated {0} = {1}", item.NomeMembro, vRef.ToValueDisplay()));
                }
                else
                {
                    MessageBox.Show(err, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnEditarVarInstancia_Click(object sender, RoutedEventArgs e) => EditarVariavel(_vm.VariavelInstanciaSelecionada, _vm.AtualizarVariaveisInstancia);
        private void DgVarInstancia_DoubleClick(object sender, MouseButtonEventArgs e) => EditarVariavel(_vm.VariavelInstanciaSelecionada, _vm.AtualizarVariaveisInstancia);

        private void BtnEditarElemArray_Click(object sender, RoutedEventArgs e) => EditarVariavel(_vm.ElementoArraySelecionado, _vm.AtualizarElementosArray);
        private void DgArrayElem_DoubleClick(object sender, MouseButtonEventArgs e) => EditarVariavel(_vm.ElementoArraySelecionado, _vm.AtualizarElementosArray);

        private void BtnAddArrayElem_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ArraySelecionado == null || !_vm.PossuiPapyrus) return;
            ArrayInfo arr = _vm.ArraySelecionado;
            arr.AddEntry(_vm.SaveAtual.Papyrus);
            PapyrusVariable added = arr.Variables[arr.Variables.Count - 1];
            _vm.SaveAtual.HasChanges = true;
            _vm.Historico.Record(
                "Add Array Entry",
                () => { arr.RemoveEntry(arr.Variables.Count - 1); },
                () => { arr.Variables.Add(added); arr.Length = arr.Variables.Count; });
            _vm.AtualizarElementosArray();
        }

        private void BtnRemArrayElem_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ArraySelecionado == null || _vm.ElementoArraySelecionado == null) return;
            ArrayInfo arr = _vm.ArraySelecionado;
            int idx = _vm.ElementoArraySelecionado.Indice;
            PapyrusVariable removed = arr.Variables[idx];
            if (arr.RemoveEntry(idx))
            {
                _vm.SaveAtual.HasChanges = true;
                _vm.Historico.Record(
                    "Remove Array Entry",
                    () => { arr.Variables.Insert(Math.Min(idx, arr.Variables.Count), removed); arr.Length = arr.Variables.Count; },
                    () => { arr.RemoveEntry(idx); });
                _vm.AtualizarElementosArray();
            }
        }

        private void BtnShiftUpArray_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ArraySelecionado == null || _vm.ElementoArraySelecionado == null) return;
            ArrayInfo arr = _vm.ArraySelecionado;
            int idx = _vm.ElementoArraySelecionado.Indice;
            if (arr.ShiftUp(idx))
            {
                _vm.SaveAtual.HasChanges = true;
                _vm.Historico.Record("Shift Array Up", () => arr.ShiftDown(idx - 1), () => arr.ShiftUp(idx));
                _vm.AtualizarElementosArray();
            }
        }

        private void BtnShiftDownArray_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ArraySelecionado == null || _vm.ElementoArraySelecionado == null) return;
            ArrayInfo arr = _vm.ArraySelecionado;
            int idx = _vm.ElementoArraySelecionado.Indice;
            if (arr.ShiftDown(idx))
            {
                _vm.SaveAtual.HasChanges = true;
                _vm.Historico.Record("Shift Array Down", () => arr.ShiftUp(idx + 1), () => arr.ShiftDown(idx));
                _vm.AtualizarElementosArray();
            }
        }

        private void BtnEncerrarThread_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ThreadSelecionada == null) return;
            if (MessageBox.Show("Terminate (NOP) selected active script thread?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _vm.EncerrarThreadSelecionada();
            }
        }

        private void BtnExcluirThread_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ThreadSelecionada == null) return;
            if (MessageBox.Show("Delete selected active script thread?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _vm.ExcluirThreadSelecionada();
            }
        }

        private void EditarStringSelecionada()
        {
            if (_vm.StringSelecionada == null || !_vm.PossuiPapyrus) return;
            int idx = _vm.StringSelecionada.Index;
            string oldText = _vm.StringSelecionada.Text;

            JanelaInputValor dlg = new JanelaInputValor("Edit String #" + idx, "String text:", oldText);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                string newText = dlg.ValorTexto;
                _vm.SaveAtual.Papyrus.Strings.EditString(idx, newText);
                _vm.SaveAtual.HasChanges = true;
                _vm.Historico.Record(
                    "Edit String #" + idx,
                    () => _vm.SaveAtual.Papyrus.Strings.EditString(idx, oldText),
                    () => _vm.SaveAtual.Papyrus.Strings.EditString(idx, newText));
                _vm.AtualizarListaStrings();
            }
        }

        private void BtnEditarString_Click(object sender, RoutedEventArgs e) => EditarStringSelecionada();
        private void DgStrings_DoubleClick(object sender, MouseButtonEventArgs e) => EditarStringSelecionada();

        private void BtnAddString_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.PossuiPapyrus) return;
            JanelaInputValor dlg = new JanelaInputValor("Add String", "New string text:", string.Empty);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                TString added = _vm.SaveAtual.Papyrus.Strings.AddString(dlg.ValorTexto);
                _vm.SaveAtual.HasChanges = true;
                _vm.AtualizarListaStrings();
                _vm.RegistrarLog("EDIT", string.Format("Added string #{0}: {1}", added.Index, added.Text));
            }
        }

        private void BtnEditarFlags_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ChangeFormSelecionado == null) return;
            ChangeForm cf = _vm.ChangeFormSelecionado;
            uint oldFlags = cf.ChangeFlags;

            JanelaEditarChangeFlags dlg = new JanelaEditarChangeFlags(oldFlags);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                uint newFlags = dlg.ResultFlags;
                cf.ChangeFlags = newFlags;
                _vm.SaveAtual.HasChanges = true;
                _vm.Historico.Record(
                    "ChangeFlags " + cf.FormIDHex,
                    () => { cf.ChangeFlags = oldFlags; },
                    () => { cf.ChangeFlags = newFlags; });
                _vm.AtualizarListaChangeForms();
                _vm.RegistrarLog("EDIT", string.Format("ChangeFlags of {0} set to 0x{1:X8}", cf.FormIDHex, newFlags));
            }
        }

        private void BtnExcluirChangeForm_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ChangeFormSelecionado == null) return;
            if (MessageBox.Show("Delete selected ChangeForm?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _vm.ExcluirChangeFormSelecionado();
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    await _vm.CarregarSaveAsync(files[0]);
                }
            }
        }
    }
}
