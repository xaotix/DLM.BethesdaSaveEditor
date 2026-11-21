using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DLM.BethesdaSaveEditor.Core;
using DLM.BethesdaSaveEditor.Ess;
using DLM.BethesdaSaveEditor.ViewModels;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaNavegadorSaves : Window
    {
        private readonly ObservableCollection<SaveSummaryItem> _itens = new ObservableCollection<SaveSummaryItem>();

        public string CaminhoSelecionado { get; private set; }

        public JanelaNavegadorSaves()
        {
            InitializeComponent();
            DgSaves.ItemsSource = _itens;
            CarregarPastasDetectadas();
        }

        private void CarregarPastasDetectadas()
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
                TxtStatus.Text = "Nenhuma pasta padrao de saves encontrada.";
            }
        }

        private async void CmbPastas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string pasta = CmbPastas.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(pasta) && Directory.Exists(pasta))
            {
                await CarregarSavesDaPastaAsync(pasta);
            }
        }

        private async void BtnEscolherPasta_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = "Selecione a pasta contendo os arquivos de save";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!CmbPastas.Items.Contains(dlg.SelectedPath))
                    {
                        CmbPastas.Items.Add(dlg.SelectedPath);
                    }
                    CmbPastas.SelectedItem = dlg.SelectedPath;
                    await CarregarSavesDaPastaAsync(dlg.SelectedPath);
                }
            }
        }

        private async Task CarregarSavesDaPastaAsync(string pasta)
        {
            _itens.Clear();
            ImgPreview.Source = null;
            TxtArquivo.Text = string.Empty;
            TxtDetalhes.Text = string.Empty;
            TxtStatus.Text = "Lendo cabecalhos...";

            List<SaveSummaryItem> encontrados = await Task.Run(() =>
            {
                List<SaveSummaryItem> lista = new List<SaveSummaryItem>();
                string[] arquivos = Directory.GetFiles(pasta, "*.*")
                    .Where(f => f.EndsWith(".ess", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".fos", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".sav0", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Take(150)
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
                        // Ignora arquivos invalidos na pasta
                    }
                }
                return lista;
            });

            foreach (SaveSummaryItem item in encontrados)
            {
                _itens.Add(item);
            }

            TxtStatus.Text = string.Format("{0} save(s) encontrado(s).", _itens.Count);
        }

        private void DgSaves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveSummaryItem item = DgSaves.SelectedItem as SaveSummaryItem;
            if (item == null) return;

            ImgPreview.Source = item.Miniatura;
            TxtArquivo.Text = item.NomeArquivo;
            TxtDetalhes.Text = string.Format("{0} - Nivel {1}\n{2}\n{3} ({4})",
                item.Personagem, item.Nivel, item.Localizacao, item.Jogo, item.TamanhoMB);
        }

        private void DgSaves_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmarSelecao();
        }

        private void BtnAbrir_Click(object sender, RoutedEventArgs e)
        {
            ConfirmarSelecao();
        }

        private void ConfirmarSelecao()
        {
            SaveSummaryItem item = DgSaves.SelectedItem as SaveSummaryItem;
            if (item == null) return;
            CaminhoSelecionado = item.CaminhoCompleto;
            DialogResult = true;
            Close();
        }
    }
}
