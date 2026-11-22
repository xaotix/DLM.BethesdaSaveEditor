using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using DLM.BethesdaSaveEditor.Core;
using DLM.BethesdaSaveEditor.Ess;
using DLM.BethesdaSaveEditor.ViewModels;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaInicial : Window
    {
        private readonly List<SaveSummaryItem> _todosSaves = new List<SaveSummaryItem>();
        private readonly ObservableCollection<SaveSummaryItem> _savesFiltrados = new ObservableCollection<SaveSummaryItem>();

        public JanelaInicial()
        {
            InitializeComponent();
            DgSaves.ItemsSource = _savesFiltrados;
            PreviewKeyDown += JanelaInicial_PreviewKeyDown;
            CarregarPastasPadrao();
        }

        private void JanelaInicial_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
            {
                e.Handled = true;
                AbrirArquivoManual();
            }
            else if (e.Key == Key.Enter && DgSaves.SelectedItem is SaveSummaryItem)
            {
                e.Handled = true;
                AbrirSaveNoEditor((SaveSummaryItem)DgSaves.SelectedItem);
            }
        }

        private void CarregarPastasPadrao()
        {
            GameType[] jogos =
            {
                GameType.SkyrimSE,
                GameType.SkyrimLE,
                GameType.SkyrimVR,
                GameType.Fallout4,
                GameType.FalloutVR
            };

            foreach (GameType j in jogos)
            {
                string pasta = j.ObterPastaSavesPadrao();
                if (Directory.Exists(pasta))
                {
                    CmbPastas.Items.Add(pasta);
                }
            }

            if (CmbPastas.Items.Count > 0)
            {
                CmbPastas.SelectedIndex = 0;
            }
            else
            {
                TxtStatus.Text = "No default save folder found. Select a folder to list saves.";
            }
        }

        private async void CmbPastas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string pasta = CmbPastas.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(pasta) && Directory.Exists(pasta))
            {
                await CarregarPastaAsync(pasta);
            }
        }

        private async void BtnEscolherPasta_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = "Select Bethesda Saves Folder";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!CmbPastas.Items.Contains(dlg.SelectedPath))
                    {
                        CmbPastas.Items.Add(dlg.SelectedPath);
                    }
                    CmbPastas.SelectedItem = dlg.SelectedPath;
                    await CarregarPastaAsync(dlg.SelectedPath);
                }
            }
        }

        private async void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            string pasta = (CmbPastas.Text ?? string.Empty).Trim();
            if (Directory.Exists(pasta))
            {
                await CarregarPastaAsync(pasta);
            }
        }

        private async Task CarregarPastaAsync(string pasta)
        {
            _todosSaves.Clear();
            _savesFiltrados.Clear();
            LimparPreview();
            TxtStatus.Text = "Scanning saves in " + pasta + "...";

            List<SaveSummaryItem> encontrados = await Task.Run(() =>
            {
                List<SaveSummaryItem> lista = new List<SaveSummaryItem>();
                string[] arquivos = Directory.GetFiles(pasta, "*.*")
                    .Where(f => f.EndsWith(".ess", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".fos", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".sav0", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Take(200)
                    .ToArray();

                foreach (string arq in arquivos)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(arq);
                        SaveHeader h = EssFile.ReadHeaderOnly(arq);
                        lista.Add(new SaveSummaryItem
                        {
                            CaminhoCompleto = arq,
                            NomeArquivo = fi.Name,
                            Jogo = h.Game.ObterNome(),
                            Personagem = h.CharacterName.Text,
                            Nivel = h.Level,
                            Localizacao = h.Location.Text,
                            DataModificacao = fi.LastWriteTime,
                            TamanhoBytes = fi.Length,
                            Miniatura = h.CreateScreenshotBitmap()
                        });
                    }
                    catch
                    {
                    }
                }
                return lista;
            });

            _todosSaves.AddRange(encontrados);
            AplicarFiltro();
            TxtStatus.Text = string.Format("{0} save(s) found.", _todosSaves.Count);
        }

        private void TxtFiltro_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltro();

        private void AplicarFiltro()
        {
            _savesFiltrados.Clear();
            string q = (TxtFiltro.Text ?? string.Empty).Trim();
            IEnumerable<SaveSummaryItem> query = _todosSaves;
            if (q.Length > 0)
            {
                query = query.Where(s => s.Personagem.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                      || s.Localizacao.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                      || s.NomeArquivo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            foreach (SaveSummaryItem s in query) _savesFiltrados.Add(s);
        }

        private void LimparPreview()
        {
            ImgPreview.Source = null;
            TxtPersonagem.Text = string.Empty;
            TxtJogo.Text = string.Empty;
            TxtDetalhes.Text = string.Empty;
            TxtArquivo.Text = string.Empty;
            BtnAbrirSelecionado.IsEnabled = false;
        }

        private void DgSaves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveSummaryItem item = DgSaves.SelectedItem as SaveSummaryItem;
            if (item == null) { LimparPreview(); return; }

            ImgPreview.Source = item.Miniatura;
            TxtPersonagem.Text = item.Personagem;
            TxtJogo.Text = item.Jogo;
            TxtDetalhes.Text = string.Format("Level: {0}\nLocation: {1}\nSize: {2}\nModified: {3:yyyy-MM-dd HH:mm}",
                item.Nivel, item.Localizacao, item.TamanhoMB, item.DataModificacao);
            TxtArquivo.Text = item.NomeArquivo;
            BtnAbrirSelecionado.IsEnabled = true;
        }

        private void DgSaves_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SaveSummaryItem item = DgSaves.SelectedItem as SaveSummaryItem;
            if (item != null) AbrirSaveNoEditor(item);
        }

        private void BtnAbrirSelecionado_Click(object sender, RoutedEventArgs e)
        {
            SaveSummaryItem item = DgSaves.SelectedItem as SaveSummaryItem;
            if (item != null) AbrirSaveNoEditor(item);
        }

        private void AbrirSaveNoEditor(SaveSummaryItem item)
        {
            AbrirCaminhoNoEditor(item.CaminhoCompleto);
        }

        private void AbrirCaminhoNoEditor(string path)
        {
            MainWindow editor = new MainWindow(path);
            editor.Owner = this;
            editor.Show();
        }

        private void AbrirArquivoManual()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Bethesda Saves (*.ess;*.fos;*.sav0)|*.ess;*.fos;*.sav0|All Files (*.*)|*.*";
            if (dlg.ShowDialog(this) == true)
            {
                AbrirCaminhoNoEditor(dlg.FileName);
            }
        }

        private void MenuAbrirArquivo_Click(object sender, RoutedEventArgs e) => AbrirArquivoManual();
        private void MenuLimpezaLote_Click(object sender, RoutedEventArgs e) { JanelaLimpezaLote w = new JanelaLimpezaLote(); w.Owner = this; w.ShowDialog(); }
        private void MenuCompararSaves_Click(object sender, RoutedEventArgs e) { JanelaCompararSaves w = new JanelaCompararSaves(); w.Owner = this; w.ShowDialog(); }
        private void MenuOpcoes_Click(object sender, RoutedEventArgs e) { JanelaOpcoes w = new JanelaOpcoes(); w.Owner = this; w.ShowDialog(); }
        private void MenuSobre_Click(object sender, RoutedEventArgs e) { JanelaSobre w = new JanelaSobre(); w.Owner = this; w.ShowDialog(); }
        private void MenuSair_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    AbrirCaminhoNoEditor(files[0]);
                }
            }
        }
    }
}

