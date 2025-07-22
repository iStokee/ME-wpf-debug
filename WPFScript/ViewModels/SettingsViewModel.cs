using MaterialDesignColors;
using MESharp.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MESharp.ViewModels
{
	public class SettingsViewModel : INotifyPropertyChanged
	{
		private bool _isDark;
		public bool IsDark
		{
			get => _isDark;
			set
			{
				if (SetProperty(ref _isDark, value))
				{
					UpdateAndSaveTheme();
				}
			}
		}

		private ISwatch _primaryColor;
		public ISwatch PrimaryColor
		{
			get => _primaryColor;
			set
			{
				if (SetProperty(ref _primaryColor, value))
				{
					UpdateAndSaveTheme();
				}
			}
		}

		private ISwatch _secondaryColor;
		public ISwatch SecondaryColor
		{
			get => _secondaryColor;
			set
			{
				if (SetProperty(ref _secondaryColor, value))
				{
					UpdateAndSaveTheme();
				}
			}
		}

		public IEnumerable<ISwatch> Swatches { get; }

		public SettingsViewModel()
		{
			Swatches = SwatchHelper.Swatches; // FIXED: Accessing static property directly using the class name  
			LoadCurrentSettings();
		}

		private void LoadCurrentSettings()
		{
			var settings = MESharp.Services.ThemeManager.LoadSettings();
			_isDark = settings.IsDark;
			_primaryColor = Swatches.FirstOrDefault(s => s.Name.Equals(settings.PrimaryColor, System.StringComparison.OrdinalIgnoreCase)) ?? Swatches.FirstOrDefault(s => s.Name == "bluegrey");
			_secondaryColor = Swatches.FirstOrDefault(s => s.Name.Equals(settings.SecondaryColor, System.StringComparison.OrdinalIgnoreCase)) ?? Swatches.FirstOrDefault(s => s.Name == "deeppurple");

			OnPropertyChanged(nameof(IsDark));
			OnPropertyChanged(nameof(PrimaryColor));
			OnPropertyChanged(nameof(SecondaryColor));
		}

		private void UpdateAndSaveTheme()
		{
			var settings = new ThemeSettings
			{
				IsDark = this.IsDark,
				PrimaryColor = this.PrimaryColor?.Name,
				SecondaryColor = this.SecondaryColor?.Name
			};

			MESharp.Services.ThemeManager.ApplyTheme(settings);
			MESharp.Services.ThemeManager.SaveSettings(settings);
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