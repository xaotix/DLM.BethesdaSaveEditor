using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using DLM.BethesdaSaveEditor.Ess;
using DLM.BethesdaSaveEditor.ViewModels;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaCompararSaves : Window
    {
        public JanelaCompararSaves(string caminhoInicialA = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(caminhoInicialA))
            {
                TxtSaveA.Text = caminhoInicialA;
            }
        }

        private string EscolherSave()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Saves Bethesda (*.ess;*.fos;*.sav0)|*.ess;*.fos;*.sav0";
            return dlg.ShowDialog(this) == true ? dlg.FileName : null;
        }

        private void BtnSelA_Click(object sender, RoutedEventArgs e)
        {
            string f = EscolherSave();
            if (f != null) TxtSaveA.Text = f;
        }

        private void BtnSelB_Click(object sender, RoutedEventArgs e)
        {
            string f = EscolherSave();
            if (f != null) TxtSaveB.Text = f;
        }

        private async void BtnComparar_Click(object sender, RoutedEventArgs e)
        {
            string pathA = TxtSaveA.Text.Trim();
            string pathB = TxtSaveB.Text.Trim();

            if (!File.Exists(pathA) || !File.Exists(pathB))
            {
                MessageBox.Show("Selecione dois arquivos de save validos.", "Comparar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnComparar.IsEnabled = false;
            TxtStatus.Text = "Analisando arquivos...";

            try
            {
                Tuple<List<SaveDiffItem>, string> resultado = await Task.Run(() =>
                {
                    EssFile a = EssFile.Load(pathA);
                    EssFile b = EssFile.Load(pathB);

                    List<SaveDiffItem> diffs = new List<SaveDiffItem>();
                    diffs.Add(CriarItem("Geral", "Personagem", a.Header.CharacterName.Text, b.Header.CharacterName.Text, "-"));
                    diffs.Add(CriarInt("Geral", "Nivel", a.Header.Level, b.Header.Level));
                    diffs.Add(CriarMb("Tamanho", "Tamanho em Disco (MB)", a.FileSizeOnDisk, b.FileSizeOnDisk));
                    diffs.Add(CriarMb("Tamanho", "Tamanho Descompactado (MB)", a.CalculateTotalUncompressedSize(), b.CalculateTotalUncompressedSize()));
                    diffs.Add(CriarInt("Plugins", "Total de Plugins", a.Plugins.TotalCount, b.Plugins.TotalCount));
                    diffs.Add(CriarInt("ChangeForms", "Quantidade de ChangeForms", a.ChangeForms.Count, b.ChangeForms.Count));
                    diffs.Add(CriarInt("Globais", "Variaveis Globais", a.GlobalVariables.Variables.Count, b.GlobalVariables.Variables.Count));

                    if (a.Papyrus != null && b.Papyrus != null)
                    {
                        diffs.Add(CriarInt("Papyrus", "Strings", a.Papyrus.Strings.Count, b.Papyrus.Strings.Count));
                        diffs.Add(CriarInt("Papyrus", "Scripts Definidos", a.Papyrus.Scripts.Count, b.Papyrus.Scripts.Count));
                        diffs.Add(CriarInt("Papyrus", "Instancias de Script", a.Papyrus.ScriptInstances.Count, b.Papyrus.ScriptInstances.Count));
                        diffs.Add(CriarInt("Papyrus", "Instancias Soltas", a.Papyrus.CountUnattachedInstances(), b.Papyrus.CountUnattachedInstances()));
                        diffs.Add(CriarInt("Papyrus", "Threads Ativas", a.Papyrus.ActiveScripts.Count, b.Papyrus.ActiveScripts.Count));
                        diffs.Add(CriarInt("Papyrus", "Arrays", a.Papyrus.Arrays.Count, b.Papyrus.Arrays.Count));
                    }

                    HashSet<string> setA = new HashSet<string>(a.Plugins.AllPlugins().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                    HashSet<string> setB = new HashSet<string>(b.Plugins.AllPlugins().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

                    List<string> adicionados = setB.Where(p => !setA.Contains(p)).OrderBy(p => p).ToList();
                    List<string> removidos = setA.Where(p => !setB.Contains(p)).OrderBy(p => p).ToList();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("=== PLUGINS ADICIONADOS NO SAVE B (" + adicionados.Count + ") ===");
                    foreach (string p in adicionados) sb.AppendLine("  + " + p);
                    sb.AppendLine();
                    sb.AppendLine("=== PLUGINS REMOVIDOS NO SAVE B (" + removidos.Count + ") ===");
                    foreach (string p in removidos) sb.AppendLine("  - " + p);

                    return Tuple.Create(diffs, sb.ToString());
                });

                DgDiff.ItemsSource = resultado.Item1;
                TxtPluginsDiff.Text = resultado.Item2;
                TxtStatus.Text = "Comparacao concluida.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Erro na comparacao.";
                MessageBox.Show("Erro ao comparar saves:\n\n" + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnComparar.IsEnabled = true;
            }
        }

        private static SaveDiffItem CriarItem(string cat, string metrica, string va, string vb, string diff)
        {
            return new SaveDiffItem { Categoria = cat, Metrica = metrica, ValorSaveA = va, ValorSaveB = vb, Diferenca = diff };
        }

        private static SaveDiffItem CriarInt(string cat, string metrica, int a, int b)
        {
            int d = b - a;
            string s = d > 0 ? ("+" + d.ToString("N0")) : d.ToString("N0");
            return CriarItem(cat, metrica, a.ToString("N0"), b.ToString("N0"), s);
        }

        private static SaveDiffItem CriarMb(string cat, string metrica, long a, long b)
        {
            double ma = a / 1048576.0;
            double mb = b / 1048576.0;
            double d = mb - ma;
            string s = (d > 0 ? "+" : string.Empty) + d.ToString("N2") + " MB";
            return CriarItem(cat, metrica, ma.ToString("N2") + " MB", mb.ToString("N2") + " MB", s);
        }
    }
}
