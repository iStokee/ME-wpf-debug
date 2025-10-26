using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace MESharp
{
	public partial class App : Application
	{
		private static CancellationTokenRegistration _shutdownRegistration;
		private static bool _shutdownRegistered;

		internal static void DisposeShutdownRegistration()
		{
			if (!_shutdownRegistered)
			{
				return;
			}

			try
			{
				_shutdownRegistration.Dispose();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"WPF: Failed to dispose shutdown registration cleanly: {ex.Message}");
			}
			finally
			{
				_shutdownRegistration = default;
				_shutdownRegistered = false;
			}
		}

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

			// Start monitoring for shutdown signals from native host
			try
			{
				MESharp.Services.ShutdownMonitor.StartMonitoring();

				// Register handler to gracefully close WPF when signaled
				DisposeShutdownRegistration();
				_shutdownRegistration = MESharp.Services.ShutdownMonitor.Token.Register(() =>
				{
					Dispatcher.InvokeAsync(() =>
					{
						Console.WriteLine("WPF: Gracefully shutting down due to runtime restart...");
						Shutdown();
					});
				});
				_shutdownRegistered = true;

				Console.WriteLine("WPF: Shutdown monitoring active.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"WPF: Failed to start shutdown monitoring: {ex.Message}");
			}

			base.OnStartup(e);
		}
	}
}
