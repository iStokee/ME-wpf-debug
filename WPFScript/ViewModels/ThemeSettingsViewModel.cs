using MESharp.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MESharp.Views;
using System.Windows;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class ThemeSettingsViewModel : INotifyPropertyChanged
    {
        private bool _suppressThemePersistence;

        private bool _isDark;
        public bool IsDark
        {
            get => _isDark;
            set
            {
                if (SetProperty(ref _isDark, value))
                {
                    if (!_suppressThemePersistence)
                    {
                        UpdateAndSaveTheme();
                    }
                }
            }
        }

        public ObservableCollection<ColorOption> AvailableColors { get; }
        public ObservableCollection<ColorOption> CustomColors { get; } = new();

        private ColorOption _selectedPrimary;
        public ColorOption SelectedPrimary
        {
            get => _selectedPrimary;
            set
            {
                if (SetProperty(ref _selectedPrimary, value))
                {
                    if (!_suppressThemePersistence)
                    {
                        UpdateAndSaveTheme();
                    }
                }
            }
        }

        // Secondary removed

        private string _pickedCustomHex;
        public string PickedCustomHex
        {
            get => _pickedCustomHex;
            set
            {
                if (SetProperty(ref _pickedCustomHex, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public RelayCommand PickCustomColorCommand { get; }
        public RelayCommand RemoveCustomColorCommand { get; }
        public RelayCommand GenerateRandomColorsCommand { get; }
        public ICommand SelectThemeCommand { get; }

        public ThemeSettingsViewModel()
        {
            AvailableColors = new ObservableCollection<ColorOption>(ColorOption.Defaults());
            PickCustomColorCommand = new RelayCommand(_ => PickCustomColor());
            RemoveCustomColorCommand = new RelayCommand(hex => RemoveCustomColor(hex as string));
            GenerateRandomColorsCommand = new RelayCommand(_ => GenerateRandomColors());
            SelectThemeCommand = new RelayCommand(opt => { if (opt is ColorOption co) SelectedPrimary = co; });
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var settings = MESharp.Services.ThemeManager.LoadSettings();

            _isDark = settings.IsDark;

            CustomColors.Clear();
            if (settings.CustomColors != null && settings.CustomColors.Count > 0)
            {
                foreach (var hex in settings.CustomColors.Distinct(System.StringComparer.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(hex)) continue;
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(hex);
                        var brush = new SolidColorBrush(color);
                        brush.Freeze();
                        CustomColors.Add(new ColorOption
                        {
                            Name = hex.ToUpperInvariant(),
                            Hex = hex,
                            Brush = brush
                        });
                    }
                    catch
                    {
                        // Ignore malformed persisted colors
                    }
                }
            }

            _selectedPrimary = FindMatchingColor(settings.PrimaryColor) ?? ColorOption.MatchOrDefault(AvailableColors, settings.PrimaryColor);

            OnPropertyChanged(nameof(IsDark));
            OnPropertyChanged(nameof(SelectedPrimary));
        }

        private void UpdateAndSaveTheme()
        {
            var settings = new ThemeSettings
            {
                IsDark = this.IsDark,
                PrimaryColor = this.SelectedPrimary?.Hex,
                CustomColors = CustomColors.Select(c => c.Hex).ToList(),
            };

            MESharp.Services.ThemeManager.ApplyTheme(settings);
            MESharp.Services.ThemeManager.SaveSettings(settings);

            // Force UI refresh to ensure theme applies instantly
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                CommandManager.InvalidateRequerySuggested();
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void PickCustomColor()
        {
            var dlg = new ColorPickerWindow(SelectedPrimary?.Hex ?? "#FF3F51B5");
            dlg.Owner = Application.Current?.MainWindow;
            if (dlg.ShowDialog() == true)
            {
                // Immediately add and save on confirm
                AddCustomColorFromHex(dlg.ResultHex);
            }
        }

        private void AddCustomColorFromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return;
            var parsed = MESharp.Services.ThemeManager.ParseColor(hex);
            if (parsed == null)
            {
                return;
            }

            var normalizedHex = $"#{parsed.Value.A:X2}{parsed.Value.R:X2}{parsed.Value.G:X2}{parsed.Value.B:X2}";
            var existing = CustomColors.FirstOrDefault(c => c.Hex.Equals(normalizedHex, System.StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (!ReferenceEquals(SelectedPrimary, existing))
                {
                    SelectedPrimary = existing;
                }
                else
                {
                    UpdateAndSaveTheme();
                }
                return;
            }

            var brush = new SolidColorBrush(parsed.Value); brush.Freeze();
            var item = new ColorOption { Name = normalizedHex, Hex = normalizedHex, Brush = brush };
            CustomColors.Add(item);
            SelectedPrimary = item;
        }

        private void RemoveCustomColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return;
            var target = CustomColors.FirstOrDefault(c => c.Hex.Equals(hex, System.StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                var removedSelected = ReferenceEquals(SelectedPrimary, target);
                CustomColors.Remove(target);
                if (removedSelected)
                {
                    _suppressThemePersistence = true;
                    SelectedPrimary = CustomColors.FirstOrDefault() ?? ColorOption.MatchOrDefault(AvailableColors, null);
                    _suppressThemePersistence = false;
                }
                UpdateAndSaveTheme();
            }
        }

        // Background color overrides removed per UX feedback

        private void GenerateRandomColors()
        {
            var rnd = new System.Random();
            int count = 5;
            for (int i = 0; i < count; i++)
            {
                byte r = (byte)rnd.Next(0, 256);
                byte g = (byte)rnd.Next(0, 256);
                byte b = (byte)rnd.Next(0, 256);
                string hex = $"#FF{r:X2}{g:X2}{b:X2}";
                if (CustomColors.Any(c => c.Hex.Equals(hex, System.StringComparison.OrdinalIgnoreCase)))
                    continue;
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color); brush.Freeze();
                CustomColors.Add(new ColorOption { Name = hex, Hex = hex, Brush = brush });
            }
            UpdateAndSaveTheme();
        }

        private ColorOption FindMatchingColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return CustomColors.FirstOrDefault(c =>
                       c.Hex.Equals(value, System.StringComparison.OrdinalIgnoreCase) ||
                       c.Name.Equals(value, System.StringComparison.OrdinalIgnoreCase));
        }

		#region INotifyPropertyChanged  
		public event PropertyChangedEventHandler PropertyChanged;
		protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
