using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaSobre : Window
    {
        public JanelaSobre()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
