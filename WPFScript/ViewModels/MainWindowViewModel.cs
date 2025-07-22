using MESharp.Commands;    // where your RelayCommand (or DelegateCommand) lives
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
	public enum AppPage
	{
		Skills,
		Inventory,
		Equipment,
		Npcs,
		Settings // Added
	}

	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private readonly DateTime _sessionStart;
		private readonly DispatcherTimer _updateTimer;

		private object _currentViewModel;
		public object CurrentViewModel
		{
			get => _currentViewModel;
			private set => SetProperty(ref _currentViewModel, value);
		}

		private AppPage _currentPage;
		public AppPage CurrentPage
		{
			get => _currentPage;
			private set
			{
				if (SetProperty(ref _currentPage, value))
				{
					OnPropertyChanged(nameof(IsSkillsSelected));
					OnPropertyChanged(nameof(IsInventorySelected));
					OnPropertyChanged(nameof(IsEquipmentSelected));
					OnPropertyChanged(nameof(IsNpcsSelected));
					OnPropertyChanged(nameof(IsSettingsSelected));
					OnPropertyChanged(nameof(CurrentPageName)); // Notify that the name has changed
				}
			}
		}

		public string CurrentPageName => $"Active Page:      {CurrentPage}";

		// one bool per view, bound to each ToggleButton.IsChecked
		public bool IsSkillsSelected => CurrentPage == AppPage.Skills;
		public bool IsInventorySelected => CurrentPage == AppPage.Inventory;
		public bool IsEquipmentSelected => CurrentPage == AppPage.Equipment;
		public bool IsNpcsSelected => CurrentPage == AppPage.Npcs;
		public bool IsSettingsSelected => CurrentPage == AppPage.Settings;

		// Declare your commands
		public ICommand ShowSkillsCommand { get; }
		public ICommand ShowInventoryCommand { get; }
		public ICommand ShowNpcsCommand { get; }
		public ICommand ShowSettingsCommand { get; }

		private string _sessionRuntimeString = "--:--:--";
		public string SessionRuntimeString
		{
			get => _sessionRuntimeString;
			private set => SetProperty(ref _sessionRuntimeString, value);
		}

		public MainWindowViewModel()
		{
			_sessionStart = DateTime.UtcNow;

			// Wire up commands to methods
			ShowSkillsCommand    = new RelayCommand(_ => ShowSkills());
			ShowInventoryCommand = new RelayCommand(_ => ShowInventory());
			ShowNpcsCommand      = new RelayCommand(_ => ShowNpcs());
			ShowSettingsCommand  = new RelayCommand(_ => ShowSettings());

			// Timer for session runtime clock
			_updateTimer = new DispatcherTimer(
				TimeSpan.FromSeconds(1),
				DispatcherPriority.Background,
				(s, e) => UpdateSessionTime(),
				Dispatcher.CurrentDispatcher
			);
			_updateTimer.Start();

			// Pick a default view
			ShowSkills();
		}

		private void UpdateSessionTime()
		{
			var elapsed = DateTime.UtcNow - _sessionStart;
			SessionRuntimeString = elapsed.ToString(@"hh\:mm\:ss");
		}

		private void ShowSkills()
		{
			CurrentViewModel = new SkillsViewModel();
			CurrentPage      = AppPage.Skills;
		}

		private void ShowInventory()
		{
			CurrentViewModel = new InventoryViewModel();
			CurrentPage      = AppPage.Inventory;
		}

		private void ShowNpcs()
		{
			CurrentViewModel = new NpcViewModel();
			CurrentPage      = AppPage.Npcs;
		}

		private void ShowSettings()
		{
			CurrentViewModel = new SettingsViewModel();
			CurrentPage = AppPage.Settings;
		}

		#region INotifyPropertyChanged boilerplate
		public event PropertyChangedEventHandler PropertyChanged;
		bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propName = null)
		{
			if (!Equals(field, newValue))
			{
				field = newValue;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
				return true;
			}
			return false;
		}
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		#endregion
	}
}