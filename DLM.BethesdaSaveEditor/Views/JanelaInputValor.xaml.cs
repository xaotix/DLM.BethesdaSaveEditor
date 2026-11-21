using System.Windows;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaInputValor : Window
    {
        public string ValorTexto => TxtValor.Text;

        public JanelaInputValor(string titulo, string prompt, string valorInicial)
        {
            InitializeComponent();
            Title = titulo;
            TxtPrompt.Text = prompt;
            TxtValor.Text = valorInicial ?? string.Empty;
            Loaded += (s, e) =>
            {
                TxtValor.Focus();
                TxtValor.SelectAll();
            };
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

