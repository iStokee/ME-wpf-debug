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
        ItemsUnified,
        GrandExchange,
        ObjectsUnified,
        Interfaces,
        DungeoneeringDebug,
        MiscApi,
        Varbit,
        Quest,
        ApiDocs,
        Knowledge,
        Mcp,
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
                    OnPropertyChanged(nameof(IsItemsUnifiedSelected));
                    OnPropertyChanged(nameof(IsGrandExchangeSelected));
                    OnPropertyChanged(nameof(IsObjectsUnifiedSelected));
                    OnPropertyChanged(nameof(IsInterfacesSelected));
                    OnPropertyChanged(nameof(IsDungeoneeringDebugSelected));
                    OnPropertyChanged(nameof(IsMiscApiSelected));
                    OnPropertyChanged(nameof(IsVarbitSelected));
                    OnPropertyChanged(nameof(IsQuestSelected));
                    OnPropertyChanged(nameof(IsApiDocsSelected));
                    OnPropertyChanged(nameof(IsKnowledgeSelected));
                    OnPropertyChanged(nameof(IsMcpSelected));
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
        public bool IsItemsUnifiedSelected => CurrentPage == AppPage.ItemsUnified;
        public bool IsGrandExchangeSelected => CurrentPage == AppPage.GrandExchange;
        public bool IsObjectsUnifiedSelected => CurrentPage == AppPage.ObjectsUnified;
        public bool IsInterfacesSelected => CurrentPage == AppPage.Interfaces;
        public bool IsDungeoneeringDebugSelected => CurrentPage == AppPage.DungeoneeringDebug;
        public bool IsMiscApiSelected => CurrentPage == AppPage.MiscApi;
        public bool IsVarbitSelected => CurrentPage == AppPage.Varbit;
        public bool IsQuestSelected => CurrentPage == AppPage.Quest;
        public bool IsApiDocsSelected => CurrentPage == AppPage.ApiDocs;
        public bool IsKnowledgeSelected => CurrentPage == AppPage.Knowledge;
        public bool IsMcpSelected => CurrentPage == AppPage.Mcp;
        public bool IsHelpSelected => CurrentPage == AppPage.Help;

        public ICommand ShowGameCommand { get; }
        public ICommand ShowChatCommand { get; }
        public ICommand ShowSkillsCommand { get; }
        public ICommand ShowPlayersCommand { get; }
        public ICommand ShowItemsUnifiedCommand { get; }
        public ICommand ShowGrandExchangeCommand { get; }
        public ICommand ShowObjectsUnifiedCommand { get; }
        public ICommand ShowInterfacesCommand { get; }
        public ICommand ShowDungeoneeringDebugCommand { get; }
        public ICommand ShowMiscApiCommand { get; }
        public ICommand ShowVarbitCommand { get; }
        public ICommand ShowQuestCommand { get; }
        public ICommand ShowApiDocsCommand { get; }
        public ICommand ShowKnowledgeCommand { get; }
        public ICommand ShowMcpCommand { get; }
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
            ShowItemsUnifiedCommand   = new RelayCommand(_ => ShowItemsUnified());
            ShowGrandExchangeCommand  = new RelayCommand(_ => ShowGrandExchange());
            ShowObjectsUnifiedCommand = new RelayCommand(_ => ShowObjectsUnified());
            ShowInterfacesCommand     = new RelayCommand(_ => ShowInterfaces());
            ShowDungeoneeringDebugCommand = new RelayCommand(_ => ShowDungeoneeringDebug());
            ShowMiscApiCommand        = new RelayCommand(_ => ShowMiscApi());
            ShowVarbitCommand         = new RelayCommand(_ => ShowVarbit());
            ShowQuestCommand          = new RelayCommand(_ => ShowQuest());
            ShowApiDocsCommand        = new RelayCommand(param => ShowApiDocs(param as string));
            ShowKnowledgeCommand      = new RelayCommand(_ => ShowKnowledge());
            ShowMcpCommand            = new RelayCommand(_ => ShowMcp());
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
        private void ShowItemsUnified() => SwitchView(AppPage.ItemsUnified, () => new ItemsUnifiedViewModel());
        private void ShowGrandExchange() => SwitchView(AppPage.GrandExchange, () => new GrandExchangeViewModel());
        private void ShowObjectsUnified() => SwitchView(AppPage.ObjectsUnified, () => new ObjectsUnifiedViewModel());
        private void ShowInterfaces() => SwitchView(AppPage.Interfaces, () => new InterfacesViewModel());
        private void ShowDungeoneeringDebug() => SwitchView(AppPage.DungeoneeringDebug, () => new DungeoneeringDebugViewModel());
        private void ShowMiscApi() => SwitchView(AppPage.MiscApi, () => new MiscApiViewModel());
        private void ShowVarbit() => SwitchView(AppPage.Varbit, () => new VarbitViewModel());
        private void ShowQuest() => SwitchView(AppPage.Quest, () => new QuestViewModel());
        private void ShowKnowledge() => SwitchView(AppPage.Knowledge, () => new KnowledgeViewModel());
        private void ShowMcp() => SwitchView(AppPage.Mcp, () => new McpViewModel());
        private void ShowApiDocs(string? initialClassName = null)
        {
            SwitchView(AppPage.ApiDocs, () => new ApiDocsViewModel(OpenApiDebugToolByClassName, initialClassName));

            if (!string.IsNullOrWhiteSpace(initialClassName) && CurrentViewModel is ApiDocsViewModel docsVm)
            {
                docsVm.SearchText = initialClassName;
            }
        }
        private void ShowHelp() => SwitchView(AppPage.Help, () => new HelpViewModel());

        private void OpenApiDebugToolByClassName(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            switch (className)
            {
                case "Game":
                    ShowGame();
                    return;
                case "Chat":
                    ShowChat();
                    return;
                case "Skills":
                    ShowSkills();
                    return;
                case "Players":
                case "LocalPlayer":
                    ShowPlayers();
                    return;
                case "Knowledge":
                case "KnowledgeItem":
                case "KnowledgeQuery":
                    ShowKnowledge();
                    return;
                case "Inventory":
                case "ItemContainers":
                case "ItemContainer":
                case "InventoryInterfaces":
                    OpenItemsContainer("Inventory");
                    return;
                case "Bank":
                    OpenItemsContainer("Bank");
                    return;
                case "Equipment":
                    OpenItemsContainer("Equipment");
                    return;
                case "Loot":
                    OpenItemsContainer("Loot");
                    return;
                case "MaterialCache":
                    OpenItemsContainer("MaterialCache");
                    return;
                case "TradeWindow":
                    OpenItemsContainer("TradeWindow");
                    return;
                case "Familiar":
                    OpenItemsContainer("Familiar");
                    return;
                case "GrandExchange":
                    ShowGrandExchange();
                    return;
                case "Objects":
                case "ActionOffsets":
                    OpenObjectsType(GameObjectType.Object);
                    return;
                case "Npcs":
                    OpenObjectsType(GameObjectType.NPC);
                    return;
                case "GroundItems":
                    OpenObjectsType(GameObjectType.GroundItem);
                    return;
                case "Interfaces":
                case "InterfaceIds":
                case "InterfaceOverrides":
                case "Dialogs":
                    ShowInterfaces();
                    return;
                case "Dungeoneering":
                    ShowDungeoneeringDebug();
                    return;
                case "Focus":
                    ShowGame();
                    return;
                case "Abilities":
                case "ActionButtons":
                case "DebugDraw":
                case "Session":
                case "ScriptHost":
                    ShowMiscApi();
                    return;
                case "Varbits":
                    ShowVarbit();
                    return;
                case "Quest":
                case "QuestInfo":
                case "QuestWatcher":
                    ShowQuest();
                    return;
                default:
                    break;
            }
        }

        private void OpenItemsContainer(string containerName)
        {
            ShowItemsUnified();
            if (CurrentViewModel is ItemsUnifiedViewModel itemsVm)
            {
                itemsVm.TrySelectContainer(containerName, autoLoad: true);
            }
        }

        private void OpenObjectsType(GameObjectType type)
        {
            ShowObjectsUnified();
            if (CurrentViewModel is ObjectsUnifiedViewModel objectsVm)
            {
                objectsVm.FocusType(type, autoLoad: true);
            }
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
