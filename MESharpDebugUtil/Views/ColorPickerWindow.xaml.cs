using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MESharp.Views
{
    public partial class ColorPickerWindow : Window
    {
        public string ResultHex { get; private set; } = "#FFFFFFFF";

        public ColorPickerWindow(string initialHex = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(initialHex))
            {
                TryApplyHex(initialHex);
            }
            else
            {
                UpdateFromRgb(63, 81, 181); // default indigo
            }
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateFromRgb((byte)Red.Value, (byte)Green.Value, (byte)Blue.Value);
        }

        private void UpdateFromRgb(byte r, byte g, byte b)
        {
            var color = Color.FromRgb(r, g, b);
            Preview.Background = new SolidColorBrush(color);
            ResultHex = $"#FF{r:X2}{g:X2}{b:X2}";
            HexText.Text = ResultHex;
        }

        private void OnApplyHex(object sender, RoutedEventArgs e)
        {
            TryApplyHex(HexText.Text);
        }

        private void TryApplyHex(string hex)
        {
            try
            {
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (hex.Length == 7) // #RRGGBB
                    hex = "#FF" + hex.Substring(1);
                var color = (Color)ColorConverter.ConvertFromString(hex);
                Preview.Background = new SolidColorBrush(color);
                HexText.Text = hex.ToUpperInvariant();
                ResultHex = HexText.Text;
                Red.Value = color.R; Green.Value = color.G; Blue.Value = color.B;
            }
            catch { }
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

