using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using MESharp.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace MESharp.Services
{
	public static class ThemeManager
	{
		private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
		private static readonly PaletteHelper PaletteHelper = new PaletteHelper();

		public static void ApplyTheme(ThemeSettings settings)
		{
			if (settings == null) return;

			// Get the theme from the application's resource dictionary
			var theme = Application.Current != null
				? Application.Current.Resources.GetTheme()
				: null;

			// Set Light/Dark  
			theme.SetBaseTheme(settings.IsDark ? BaseTheme.Dark : BaseTheme.Light);

			// Set Primary Color  
			if (!string.IsNullOrEmpty(settings.PrimaryColor))
			{
				var primarySwatch = SwatchHelper.Swatches.FirstOrDefault(s => s.Name.Equals(settings.PrimaryColor, StringComparison.OrdinalIgnoreCase));
				if (primarySwatch != null)
				{
					// Fix: Use a valid color from the swatch's Lookup dictionary  
					var primaryColor = primarySwatch.Lookup.FirstOrDefault().Value;
					theme.SetPrimaryColor(primaryColor);
				}
			}

			// Set Secondary Color  
			if (!string.IsNullOrEmpty(settings.SecondaryColor))
			{
				var secondarySwatch = SwatchHelper.Swatches.FirstOrDefault(s => s.Name.Equals(settings.SecondaryColor, StringComparison.OrdinalIgnoreCase));
				if (secondarySwatch != null)
				{
					// Fix: Use a valid color from the swatch's Lookup dictionary for secondary color  
					var secondaryColor = secondarySwatch.Lookup.FirstOrDefault().Value;
					theme.SetSecondaryColor(secondaryColor);
				}
			}

			PaletteHelper.SetTheme(theme);
		}

		public static void SaveSettings(ThemeSettings settings)
		{
			try
			{
				string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(SettingsFilePath, json);
			}
			catch (Exception)
			{
				// Handle exceptions (e.g., logging)  
			}
		}

		public static ThemeSettings LoadSettings()
		{
			if (!File.Exists(SettingsFilePath))
			{
				return new ThemeSettings { IsDark = false, PrimaryColor = "Purple", SecondaryColor = "DeepPurple" };
			}

			try
			{
				string json = File.ReadAllText(SettingsFilePath);
				return JsonSerializer.Deserialize<ThemeSettings>(json);
			}
			catch (Exception)
			{
				return new ThemeSettings { IsDark = false, PrimaryColor = "Purple", SecondaryColor = "DeepPurple" };
			}
		}
	}
}