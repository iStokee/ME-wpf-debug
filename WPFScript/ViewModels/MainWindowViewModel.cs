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
		Game,
		Chat,
		Skills,
		Inventory,
		Equipment,
		Npcs,
		Objects,
		Bank,
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
					OnPropertyChanged(nameof(IsGameSelected));
					OnPropertyChanged(nameof(IsChatSelected));
					OnPropertyChanged(nameof(IsSkillsSelected));
					OnPropertyChanged(nameof(IsInventorySelected));
					OnPropertyChanged(nameof(IsEquipmentSelected));
					OnPropertyChanged(nameof(IsNpcsSelected));
					OnPropertyChanged(nameof(IsObjectsSelected));
					OnPropertyChanged(nameof(IsBankSelected));
					OnPropertyChanged(nameof(IsSettingsSelected));
					OnPropertyChanged(nameof(CurrentPageName)); // Notify that the name has changed
				}
			}
		}

		public string CurrentPageName => $"Active Page:      {CurrentPage}";

		// one bool per view, bound to each ToggleButton.IsChecked
		public bool IsGameSelected => CurrentPage == AppPage.Game;
		public bool IsChatSelected => CurrentPage == AppPage.Chat;
		public bool IsSkillsSelected => CurrentPage == AppPage.Skills;
		public bool IsInventorySelected => CurrentPage == AppPage.Inventory;
		public bool IsEquipmentSelected => CurrentPage == AppPage.Equipment;
		public bool IsNpcsSelected => CurrentPage == AppPage.Npcs;
		public bool IsObjectsSelected => CurrentPage == AppPage.Objects;
		public bool IsBankSelected => CurrentPage == AppPage.Bank;
		public bool IsSettingsSelected => CurrentPage == AppPage.Settings;

		// Declare your commands
		public ICommand ShowGameCommand { get; }
		public ICommand ShowChatCommand { get; }
		public ICommand ShowSkillsCommand { get; }
		public ICommand ShowInventoryCommand { get; }
		public ICommand ShowEquipmentCommand { get; }
		public ICommand ShowNpcsCommand { get; }
		public ICommand ShowObjectsCommand { get; }
		public ICommand ShowBankCommand { get; }
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
			ShowGameCommand      = new RelayCommand(_ => ShowGame());
			ShowChatCommand      = new RelayCommand(_ => ShowChat());
			ShowSkillsCommand    = new RelayCommand(_ => ShowSkills());
			ShowInventoryCommand = new RelayCommand(_ => ShowInventory());
			ShowEquipmentCommand = new RelayCommand(_ => ShowEquipment());
			ShowNpcsCommand      = new RelayCommand(_ => ShowNpcs());
			ShowObjectsCommand   = new RelayCommand(_ => ShowObjects());
			ShowBankCommand      = new RelayCommand(_ => ShowBank());
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
			ShowGame();
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

		private void ShowEquipment()
		{
			CurrentViewModel = new EquipmentViewModel();
			CurrentPage      = AppPage.Equipment;
		}

		private void ShowNpcs()
		{
			CurrentViewModel = new NpcViewModel();
			CurrentPage      = AppPage.Npcs;
		}

		private void ShowObjects()
		{
			CurrentViewModel = new ObjectsViewModel();
			CurrentPage      = AppPage.Objects;
		}

		private void ShowBank()
		{
			CurrentViewModel = new BankViewModel();
			CurrentPage      = AppPage.Bank;
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


		private void ShowGame()
		{
			CurrentViewModel = new GameViewModel();
			CurrentPage      = AppPage.Game;
		}

		private void ShowChat()
		{
			CurrentViewModel = new ChatViewModel();
			CurrentPage      = AppPage.Chat;
		}
	}
}
