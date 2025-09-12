using MESharp.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace MESharp.Services
{
    public static class ThemeManager
    {
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private static Uri GetThemePackUri(string relative)
        {
            var asm = Application.ResourceAssembly ?? typeof(ThemeManager).Assembly;
            var asmName = asm.GetName().Name;
            return new Uri($"pack://application:,,,/{asmName};component/{relative}", UriKind.Absolute);
        }

        public static void ApplyTheme(ThemeSettings settings)
        {
            if (settings == null) return;

            var app = Application.Current;
            if (app == null) return;

            // 1) Swap base theme dictionary (Light/Dark)
            var merged = app.Resources.MergedDictionaries;
            // Remove any existing theme dicts
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var src = merged[i].Source?.ToString() ?? string.Empty;
                if (src.EndsWith("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                    src.EndsWith("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                    src.Contains("/Themes/Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                    src.Contains("/Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    merged.RemoveAt(i);
                }
            }
            var baseThemePath = settings.IsDark ? "Themes/Dark.xaml" : "Themes/Light.xaml";
            merged.Add(new ResourceDictionary { Source = GetThemePackUri(baseThemePath) });

            // 2) Apply Primary color (will override the defaults from theme)
            var primary = ParseColor(settings.PrimaryColor) ?? (settings.IsDark ? (Color)ColorConverter.ConvertFromString("#FF7AA2FF") : (Color)ColorConverter.ConvertFromString("#FF3F51B5"));

            var primaryBrush = new SolidColorBrush(primary);
            primaryBrush.Freeze();

            var primaryFg = new SolidColorBrush(GetIdealForeground(primary));
            primaryFg.Freeze();

            // derive a soft tint of primary for selections (about 20% alpha)
            var primarySoft = Color.FromArgb(0x33, primary.R, primary.G, primary.B);
            var primarySoftBrush = new SolidColorBrush(primarySoft);
            primarySoftBrush.Freeze();

            app.Resources["PrimaryBrush"] = primaryBrush;
            app.Resources["PrimaryForegroundBrush"] = primaryFg;
            app.Resources["PrimarySoftBrush"] = primarySoftBrush;

            // Custom background overrides removed per UX feedback
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
                return new ThemeSettings { IsDark = false, PrimaryColor = "#3F51B5" };
            }

			try
			{
				string json = File.ReadAllText(SettingsFilePath);
				return JsonSerializer.Deserialize<ThemeSettings>(json);
			}
            catch (Exception)
            {
                return new ThemeSettings { IsDark = false, PrimaryColor = "#3F51B5" };
            }
        }

        private static Color GetIdealForeground(Color bg)
        {
            // Perceived luminance (WCAG 2.0)
            double L = 0.2126 * bg.ScR + 0.7152 * bg.ScG + 0.0722 * bg.ScB;
            return L > 0.5 ? Colors.Black : Colors.White;
        }

        public static Color? ParseColor(string? nameOrHex)
        {
            if (string.IsNullOrWhiteSpace(nameOrHex)) return null;
            try
            {
                var obj = ColorConverter.ConvertFromString(nameOrHex.Trim());
                if (obj is Color c) return c;
            }
            catch { }
            return null;
        }
    }
}
