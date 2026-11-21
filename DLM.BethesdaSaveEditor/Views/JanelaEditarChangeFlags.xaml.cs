using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace DLM.BethesdaSaveEditor.Views
{
    public partial class JanelaEditarChangeFlags : Window
    {
        private readonly CheckBox[] _bits = new CheckBox[32];
        private bool _updating;

        public uint ResultFlags { get; private set; }

        public JanelaEditarChangeFlags(uint initialFlags)
        {
            InitializeComponent();
            ResultFlags = initialFlags;

            for (int i = 31; i >= 0; i--)
            {
                CheckBox cb = new CheckBox();
                cb.Content = "Bit " + i;
                cb.Tag = i;
                cb.Checked += Bit_Changed;
                cb.Unchecked += Bit_Changed;
                _bits[i] = cb;
                GridBits.Children.Add(cb);
            }

            SyncFromFlags(initialFlags);
        }

        private void SyncFromFlags(uint flags)
        {
            _updating = true;
            ResultFlags = flags;
            TxtHex.Text = flags.ToString("X8");
            for (int i = 0; i < 32; i++)
            {
                _bits[i].IsChecked = ((flags & (1u << i)) != 0);
            }
            _updating = false;
        }

        private void Bit_Changed(object sender, RoutedEventArgs e)
        {
            if (_updating) return;
            uint flags = 0;
            for (int i = 0; i < 32; i++)
            {
                if (_bits[i].IsChecked == true)
                {
                    flags |= (1u << i);
                }
            }
            _updating = true;
            ResultFlags = flags;
            TxtHex.Text = flags.ToString("X8");
            _updating = false;
        }

        private void TxtHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            string s = (TxtHex.Text ?? string.Empty).Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            uint parsed;
            if (uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
            {
                _updating = true;
                ResultFlags = parsed;
                for (int i = 0; i < 32; i++)
                {
                    _bits[i].IsChecked = ((parsed & (1u << i)) != 0);
                }
                _updating = false;
            }
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

