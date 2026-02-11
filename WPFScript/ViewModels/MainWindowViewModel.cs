using MESharp.Commands;
using MESharp.ViewModels;
using System;
using System.Collections.Generic;
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
        Players,
        Navigation,
        ItemsUnified,
        GrandExchange,
        ObjectsUnified,
        Interfaces,
        Varbit,
        ApiDocs,
        Help
    }

    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DateTime _sessionStart;
        private readonly DispatcherTimer _updateTimer;
        private readonly EventHandler _sessionTimerHandler;
        private readonly Dictionary<AppPage, object> _viewModelCache = new();

        private object? _currentViewModel;
        public object? CurrentViewModel
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
                    OnPropertyChanged(nameof(IsPlayersSelected));
                    OnPropertyChanged(nameof(IsNavigationSelected));
                    OnPropertyChanged(nameof(IsItemsUnifiedSelected));
                    OnPropertyChanged(nameof(IsGrandExchangeSelected));
                    OnPropertyChanged(nameof(IsObjectsUnifiedSelected));
                    OnPropertyChanged(nameof(IsInterfacesSelected));
                    OnPropertyChanged(nameof(IsVarbitSelected));
                    OnPropertyChanged(nameof(IsApiDocsSelected));
                    OnPropertyChanged(nameof(IsHelpSelected));
                    OnPropertyChanged(nameof(CurrentPageName));
                    OnPropertyChanged(nameof(IsSidePanelVisible));
                }
            }
        }

        public bool IsSidePanelVisible => CurrentPage == AppPage.ItemsUnified || CurrentPage == AppPage.ObjectsUnified;
        public string CurrentPageName => $"Active Page:      {CurrentPage}";

        public bool IsGameSelected => CurrentPage == AppPage.Game;
        public bool IsChatSelected => CurrentPage == AppPage.Chat;
        public bool IsSkillsSelected => CurrentPage == AppPage.Skills;
        public bool IsPlayersSelected => CurrentPage == AppPage.Players;
        public bool IsNavigationSelected => CurrentPage == AppPage.Navigation;
        public bool IsItemsUnifiedSelected => CurrentPage == AppPage.ItemsUnified;
        public bool IsGrandExchangeSelected => CurrentPage == AppPage.GrandExchange;
        public bool IsObjectsUnifiedSelected => CurrentPage == AppPage.ObjectsUnified;
        public bool IsInterfacesSelected => CurrentPage == AppPage.Interfaces;
        public bool IsVarbitSelected => CurrentPage == AppPage.Varbit;
        public bool IsApiDocsSelected => CurrentPage == AppPage.ApiDocs;
        public bool IsHelpSelected => CurrentPage == AppPage.Help;

        public ICommand ShowGameCommand { get; }
        public ICommand ShowChatCommand { get; }
        public ICommand ShowSkillsCommand { get; }
        public ICommand ShowPlayersCommand { get; }
        public ICommand ShowNavigationCommand { get; }
        public ICommand ShowItemsUnifiedCommand { get; }
        public ICommand ShowGrandExchangeCommand { get; }
        public ICommand ShowObjectsUnifiedCommand { get; }
        public ICommand ShowInterfacesCommand { get; }
        public ICommand ShowVarbitCommand { get; }
        public ICommand ShowApiDocsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        private string _sessionRuntimeString = "--:--:--";
        public string SessionRuntimeString
        {
            get => _sessionRuntimeString;
            private set => SetProperty(ref _sessionRuntimeString, value);
        }

        public MainWindowViewModel()
        {
            _sessionStart = DateTime.UtcNow;

            ShowGameCommand           = new RelayCommand(_ => ShowGame());
            ShowChatCommand           = new RelayCommand(_ => ShowChat());
            ShowSkillsCommand         = new RelayCommand(_ => ShowSkills());
            ShowPlayersCommand        = new RelayCommand(_ => ShowPlayers());
            ShowNavigationCommand     = new RelayCommand(_ => ShowNavigation());
            ShowItemsUnifiedCommand   = new RelayCommand(_ => ShowItemsUnified());
            ShowGrandExchangeCommand  = new RelayCommand(_ => ShowGrandExchange());
            ShowObjectsUnifiedCommand = new RelayCommand(_ => ShowObjectsUnified());
            ShowInterfacesCommand     = new RelayCommand(_ => ShowInterfaces());
            ShowVarbitCommand         = new RelayCommand(_ => ShowVarbit());
            ShowApiDocsCommand        = new RelayCommand(_ => ShowApiDocs());
            ShowHelpCommand           = new RelayCommand(_ => ShowHelp());

            _sessionTimerHandler = (_, __) => UpdateSessionTime();
            _updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += _sessionTimerHandler;
            _updateTimer.Start();

            ShowGame();
        }

        private void UpdateSessionTime()
        {
            var elapsed = DateTime.UtcNow - _sessionStart;
            SessionRuntimeString = elapsed.ToString(@"hh\:mm\:ss");
        }

        private void ShowGame() => SwitchView(AppPage.Game, () => new GameViewModel());
        private void ShowChat() => SwitchView(AppPage.Chat, () => new ChatViewModel());
        private void ShowSkills() => SwitchView(AppPage.Skills, () => new SkillsViewModel());
        private void ShowPlayers() => SwitchView(AppPage.Players, () => new PlayersViewModel());
        private void ShowNavigation() => SwitchView(AppPage.Navigation, () => new NavigationViewModel());
        private void ShowItemsUnified() => SwitchView(AppPage.ItemsUnified, () => new ItemsUnifiedViewModel());
        private void ShowGrandExchange() => SwitchView(AppPage.GrandExchange, () => new GrandExchangeViewModel());
        private void ShowObjectsUnified() => SwitchView(AppPage.ObjectsUnified, () => new ObjectsUnifiedViewModel());
        private void ShowInterfaces() => SwitchView(AppPage.Interfaces, () => new InterfacesViewModel());
        private void ShowVarbit() => SwitchView(AppPage.Varbit, () => new VarbitViewModel());
        private void ShowApiDocs() => SwitchView(AppPage.ApiDocs, () => new ApiDocsViewModel());
        private void ShowHelp() => SwitchView(AppPage.Help, () => new HelpViewModel());

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

        #region INotifyPropertyChanged boilerplate
        public event PropertyChangedEventHandler? PropertyChanged;
        bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));
                return true;
            }
            return false;
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
        #endregion

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
