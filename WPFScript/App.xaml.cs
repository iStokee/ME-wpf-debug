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
			RegisterShutdownHandler();

			base.OnStartup(e);
		}

		/// <summary>
		/// Registers the shutdown handler. Called on startup and when reusing Application.Current.
		/// </summary>
		internal void RegisterShutdownHandler()
		{
			try
			{
				MESharp.Services.ShutdownMonitor.StartMonitoring();

				// Dispose old registration if it exists
				DisposeShutdownRegistration();

				// Register handler to gracefully close WPF when signaled
				_shutdownRegistration = MESharp.Services.ShutdownMonitor.Token.Register(() =>
				{
					Dispatcher.InvokeAsync(() =>
					{
						Console.WriteLine("WPF: Gracefully shutting down due to runtime restart...");

						// CRITICAL: DON'T call Shutdown() - it destroys Application.Current!
						// Instead, close all windows which will exit app.Run() naturally.
						// This preserves Application.Current for hot reload.
						foreach (Window window in Windows)
						{
							try { window.Close(); } catch { /* ignore */ }
						}

						// If we're pumping a dispatcher on a worker thread (reload case), shut it down
						var currentDispatcher = System.Windows.Threading.Dispatcher.FromThread(System.Threading.Thread.CurrentThread);
						if (currentDispatcher != null && currentDispatcher != Dispatcher && !currentDispatcher.HasShutdownStarted)
						{
							currentDispatcher.InvokeShutdown();
						}
					});
				});
				_shutdownRegistered = true;

				Console.WriteLine("WPF: Shutdown monitoring active.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"WPF: Failed to register shutdown handler: {ex.Message}");
			}
		}
	}
}
