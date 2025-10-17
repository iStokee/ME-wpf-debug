using System.Windows;

namespace MESharp
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			// Load and apply saved theme BEFORE any window is created
			// This prevents the white/wrong color mouseover flash on startup
			try
			{
				var settings = MESharp.Services.ThemeManager.LoadSettings();
				MESharp.Services.ThemeManager.ApplyTheme(settings);
			}
			catch { /* ignore theme init issues */ }

			base.OnStartup(e);
		}
	}
}
