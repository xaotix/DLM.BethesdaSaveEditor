using System.Windows;
using System.Windows.Controls;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaOpcoes : Window
    {
        private bool _carregando = true;

        public JanelaOpcoes()
        {
            InitializeComponent();
            string lang = LocalizationService.Instance.CurrentLanguage;
            foreach (ComboBoxItem item in CmbIdioma.Items)
            {
                if ((item.Tag as string) == lang)
                {
                    CmbIdioma.SelectedItem = item;
                    break;
                }
            }
            _carregando = false;
        }

        private void CmbIdioma_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_carregando) return;
            ComboBoxItem item = CmbIdioma.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag is string)
            {
                LocalizationService.Instance.CurrentLanguage = (string)item.Tag;
            }
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

