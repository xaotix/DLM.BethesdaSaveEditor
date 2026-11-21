using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DLM.BethesdaSaveEditor.Ess;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaLimpezaLote : Window
    {
        public JanelaLimpezaLote()
        {
            InitializeComponent();
        }

        private void BtnProcurar_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TxtPasta.Text = dlg.SelectedPath;
                }
            }
        }

        private async void BtnExecutar_Click(object sender, RoutedEventArgs e)
        {
            string pasta = TxtPasta.Text.Trim();
            if (!Directory.Exists(pasta))
            {
                MessageBox.Show("Selecione uma pasta valida.", "Limpeza em Lote", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool doUnattached = ChkUnattached.IsChecked == true;
            bool doUndefined = ChkUndefined.IsChecked == true;
            bool doNonExistent = ChkNonExistent.IsChecked == true;
            bool doBackup = ChkBackup.IsChecked == true;

            string[] arquivos = Directory.GetFiles(pasta, "*.*")
                .Where(f => f.EndsWith(".ess", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".fos", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (arquivos.Length == 0)
            {
                MessageBox.Show("Nenhum arquivo .ess ou .fos encontrado na pasta.", "Limpeza em Lote", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BtnExecutar.IsEnabled = false;
            TxtRelatorio.Clear();
            PrgLote.Maximum = arquivos.Length;
            PrgLote.Value = 0;

            StringBuilder log = new StringBuilder();
            int limpos = 0;

            await Task.Run(() =>
            {
                for (int i = 0; i < arquivos.Length; i++)
                {
                    string arq = arquivos[i];
                    string nome = Path.GetFileName(arq);
                    try
                    {
                        EssFile ess = EssFile.Load(arq);
                        int rem = 0;
                        if (doUnattached) rem += ess.RemoveUnattachedInstances();
                        if (doUndefined) rem += ess.RemoveUndefinedElements();
                        if (doNonExistent) rem += ess.RemoveNonExistentCreatedInstances();

                        if (rem > 0)
                        {
                            ess.Save(arq, doBackup);
                            limpos++;
                            log.AppendLine(string.Format("[LIMPO] {0} -> {1} elemento(s) tratado(s).", nome, rem));
                        }
                        else
                        {
                            log.AppendLine(string.Format("[OK]    {0} -> nenhum problema encontrado.", nome));
                        }
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine(string.Format("[ERRO]  {0} -> {1}", nome, ex.Message));
                    }

                    int idx = i + 1;
                    string parcial = log.ToString();
                    Dispatcher.Invoke(() =>
                    {
                        PrgLote.Value = idx;
                        TxtRelatorio.Text = parcial;
                        TxtRelatorio.ScrollToEnd();
                    });
                }
            });

            log.AppendLine();
            log.AppendLine(string.Format("Concluido: {0}/{1} arquivo(s) otimizado(s).", limpos, arquivos.Length));
            TxtRelatorio.Text = log.ToString();
            BtnExecutar.IsEnabled = true;
        }
    }
}
