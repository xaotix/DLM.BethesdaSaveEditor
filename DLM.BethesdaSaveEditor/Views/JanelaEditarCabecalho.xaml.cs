using System;
using System.Globalization;
using System.Windows;
using DLM.BethesdaSaveEditor.Core;
using DLM.BethesdaSaveEditor.Ess;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaEditarCabecalho : Window
    {
        private readonly EssFile _ess;

        public JanelaEditarCabecalho(EssFile ess)
        {
            InitializeComponent();
            _ess = ess;

            TxtNome.Text = ess.Header.CharacterName.Text;
            TxtNivel.Text = ess.Header.Level.ToString();
            TxtNumeroSave.Text = ess.Header.SaveNumber.ToString();
            TxtLocalizacao.Text = ess.Header.Location.Text;
            TxtRaca.Text = ess.Header.RaceId.Text;
            CmbGenero.SelectedIndex = ess.Header.Sex == 0 ? 0 : 1;
            TxtXpAtual.Text = ess.Header.CurrentXp.ToString(CultureInfo.InvariantCulture);
            TxtXpNecessario.Text = ess.Header.NeededXp.ToString(CultureInfo.InvariantCulture);

            CmbCompressao.ItemsSource = Enum.GetValues(typeof(CompressionType));
            CmbCompressao.SelectedItem = ess.Header.Compression;
            CmbCompressao.IsEnabled = ess.Header.Game.SuportaCompressao();

            if (ess.PlayerLocation != null)
            {
                TxtPosX.Text = ess.PlayerLocation.PosX.ToString("G7", CultureInfo.InvariantCulture);
                TxtPosY.Text = ess.PlayerLocation.PosY.ToString("G7", CultureInfo.InvariantCulture);
                TxtPosZ.Text = ess.PlayerLocation.PosZ.ToString("G7", CultureInfo.InvariantCulture);
                TxtCellX.Text = ess.PlayerLocation.CellX.ToString();
                TxtCellY.Text = ess.PlayerLocation.CellY.ToString();
            }
            else
            {
                TabCoords.IsEnabled = false;
            }
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            int nivel, numSave;
            float xpAtual, xpNec;

            if (!int.TryParse(TxtNivel.Text, out nivel) || nivel < 1
                || !int.TryParse(TxtNumeroSave.Text, out numSave) || numSave < 0
                || !float.TryParse(TxtXpAtual.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out xpAtual)
                || !float.TryParse(TxtXpNecessario.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out xpNec))
            {
                MessageBox.Show("Check numeric fields (Level, Save Number, XP).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_ess.PlayerLocation != null)
            {
                float px, py, pz;
                int cx, cy;
                if (!float.TryParse(TxtPosX.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out px)
                    || !float.TryParse(TxtPosY.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out py)
                    || !float.TryParse(TxtPosZ.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out pz)
                    || !int.TryParse(TxtCellX.Text, out cx)
                    || !int.TryParse(TxtCellY.Text, out cy))
                {
                    MessageBox.Show("Invalid player coordinates.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _ess.PlayerLocation.PosX = px;
                _ess.PlayerLocation.PosY = py;
                _ess.PlayerLocation.PosZ = pz;
                _ess.PlayerLocation.CellX = cx;
                _ess.PlayerLocation.CellY = cy;
            }

            _ess.Header.CharacterName.Text = TxtNome.Text.Trim();
            _ess.Header.Level = nivel;
            _ess.Header.SaveNumber = numSave;
            _ess.Header.Location.Text = TxtLocalizacao.Text.Trim();
            _ess.Header.RaceId.Text = TxtRaca.Text.Trim();
            _ess.Header.Sex = (short)(CmbGenero.SelectedIndex == 0 ? 0 : 1);
            _ess.Header.CurrentXp = xpAtual;
            _ess.Header.NeededXp = xpNec;

            if (CmbCompressao.IsEnabled && CmbCompressao.SelectedItem is CompressionType)
            {
                _ess.Header.Compression = (CompressionType)CmbCompressao.SelectedItem;
            }

            DialogResult = true;
            Close();
        }
    }
}
