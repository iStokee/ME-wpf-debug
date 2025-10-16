using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using MESharp.Commands;

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

	public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
	{
		private readonly DateTime _sessionStart;
		private readonly DispatcherTimer _updateTimer;
        private readonly EventHandler _sessionTimerHandler;
        private readonly Dictionary<AppPage, object> _viewModelCache = new();

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
            _sessionTimerHandler = (_, __) => UpdateSessionTime();
			_updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += _sessionTimerHandler;
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
			SwitchView(AppPage.Skills, () => new SkillsViewModel());
		}

		private void ShowInventory()
		{
			SwitchView(AppPage.Inventory, () => new InventoryViewModel());
		}

		private void ShowEquipment()
		{
			SwitchView(AppPage.Equipment, () => new EquipmentViewModel());
		}

		private void ShowNpcs()
		{
			SwitchView(AppPage.Npcs, () => new NpcViewModel());
		}

		private void ShowObjects()
		{
			SwitchView(AppPage.Objects, () => new ObjectsViewModel());
		}

		private void ShowBank()
		{
			SwitchView(AppPage.Bank, () => new BankViewModel());
		}

		private void ShowSettings()
		{
			SwitchView(AppPage.Settings, () => new SettingsViewModel());
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
			SwitchView(AppPage.Game, () => new GameViewModel());
		}

		private void ShowChat()
		{
			SwitchView(AppPage.Chat, () => new ChatViewModel());
		}

		private void SwitchView(AppPage page, Func<object> factory)
		{
            if (page == CurrentPage && CurrentViewModel != null)
            {
                if (CurrentViewModel is IActivatableViewModel alreadyActive)
                {
                    try { alreadyActive.OnActivated(); } catch { /* ignore */ }
                }
                return;
            }

            if (_currentViewModel is IActivatableViewModel toDeactivate)
            {
                try { toDeactivate.OnDeactivated(); } catch { /* ignore */ }
            }

            var next = GetOrCreateViewModel(page, factory);
            CurrentViewModel = next;
            CurrentPage = page;

            if (next is IActivatableViewModel toActivate)
            {
                try { toActivate.OnActivated(); } catch { /* ignore */ }
            }
		}

        private object GetOrCreateViewModel(AppPage page, Func<object> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            if (!_viewModelCache.TryGetValue(page, out var vm))
            {
                vm = factory();
                _viewModelCache[page] = vm;
            }
            return vm;
        }

        public void Dispose()
        {
            try { _updateTimer.Stop(); } catch { /* ignore */ }
            _updateTimer.Tick -= _sessionTimerHandler;

            foreach (var vm in _viewModelCache.Values)
            {
                if (vm is IActivatableViewModel activatable)
                {
                    try { activatable.OnDeactivated(); } catch { /* ignore */ }
                }

                if (vm is IDisposable disposable)
                {
                    try { disposable.Dispose(); } catch { /* ignore */ }
                }
            }

            _viewModelCache.Clear();
            CurrentViewModel = null;
        }
	}
}
