using MESharp.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class InterfaceComponentViewModel : INotifyPropertyChanged
    {
        public InterfaceComponent Component { get; }
        public ObservableCollection<InterfaceComponentViewModel> Children { get; } = new ObservableCollection<InterfaceComponentViewModel>();
        public InterfaceComponentViewModel(InterfaceComponent component)
        {
            Component = component;
        }

        public string DisplayText => $"[{Component.Index}] {Component.Id1}:{Component.Id2}:{Component.Id3}";
        public string CoordinatesText => $"X:{Component.X} Y:{Component.Y} W:{Component.Width} H:{Component.Height}";
        public string ItemText => Component.ItemId > 0 ? $"Item: {Component.ItemId} x{Component.ItemStack}" : string.Empty;
        public bool HasText => !string.IsNullOrWhiteSpace(Component.TextItem) || !string.IsNullOrWhiteSpace(Component.TextIds);
        public bool HasItem => Component.ItemId > 0;
        public string CleanTextItem => InterfaceTextCleaner.Clean(Component.TextItem);
        public string Text
        {
            get
            {
                var ids = Component.TextIds?.Trim();
                var textItem = CleanTextItem;
                if (string.IsNullOrWhiteSpace(ids))
                {
                    return textItem ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(textItem))
                {
                    return ids;
                }

                return $"{ids} {textItem}";
            }
        }
        public string MemLocText => $"Mem: 0x{Component.MemLoc:X}";
        public string IdPathText => string.IsNullOrWhiteSpace(Component.FullIdPath) ? string.Empty : Component.FullIdPath;


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class InterfacesViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
    {
        private const string InterfaceHighlightKey = "InterfaceSelection";

        public sealed class InterfaceOverrideEntry
        {
            public string Key { get; }
            public InterfaceId Id { get; }

            public InterfaceOverrideEntry(string key, InterfaceId id)
            {
                Key = key;
                Id = id;
            }

            public string Display => $"{Key} = {Id.Id1}:{Id.Id2}:{Id.Id3}";
        }

        private readonly DispatcherTimer _updateTimer;
        private bool _isActive;
        private bool _disposed;

        private bool _autoRefresh;
        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                if (SetProperty(ref _autoRefresh, value))
                {
                    UpdateTimer();
                }
            }
        }

        private bool _freezeRefresh;
        public bool FreezeRefresh
        {
            get => _freezeRefresh;
            set
            {
                if (SetProperty(ref _freezeRefresh, value))
                {
                    UpdateTimer();
                }
            }
        }

        private bool _includeHidden;
        public bool IncludeHidden
        {
            get => _includeHidden;
            set => SetProperty(ref _includeHidden, value);
        }

        private bool _textOnly;
        public bool TextOnly
        {
            get => _textOnly;
            set => SetProperty(ref _textOnly, value);
        }

        private bool _filterIndex1;
        public bool FilterIndex1
        {
            get => _filterIndex1;
            set => SetProperty(ref _filterIndex1, value);
        }

        private bool _filterIndex2;
        public bool FilterIndex2
        {
            get => _filterIndex2;
            set => SetProperty(ref _filterIndex2, value);
        }

        private bool _filterIndex3;
        public bool FilterIndex3
        {
            get => _filterIndex3;
            set => SetProperty(ref _filterIndex3, value);
        }

        private bool _focusRoot;
        public bool FocusRoot
        {
            get => _focusRoot;
            set => SetProperty(ref _focusRoot, value);
        }

        private string _rootIdText;
        public string RootIdText
        {
            get => _rootIdText;
            set => SetProperty(ref _rootIdText, value);
        }

        private InterfaceComponent _selectedInterface;
        public InterfaceComponent SelectedInterface
        {
            get => _selectedInterface;
            set
            {
                if (ReferenceEquals(_selectedInterface, value))
                {
                    return;
                }

                var previous = _selectedInterface;
                _selectedInterface = value;

                OnPropertyChanged(nameof(SelectedInterface));
                OnPropertyChanged(nameof(SelectedInterfaceLabel));
                OnPropertyChanged(nameof(SelectedInterfaceTextItem));

                var changedIdentity = !HasSameIdentity(previous, value);
                if (changedIdentity)
                {
                    if (value == null)
                    {
                        DebugDraw.Clear(InterfaceHighlightKey);
                    }
                    else
                    {
                        HighlightSelectionNow();
                    }
                }

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedInterfaceLabel =>
            SelectedInterface == null
                ? "(none selected)"
                : $"{SelectedInterface.Id1}:{SelectedInterface.Id2}:{SelectedInterface.Id3}";

        public string SelectedInterfaceTextItem => InterfaceTextCleaner.Clean(SelectedInterface?.TextItem);

        public ObservableCollection<InterfaceComponentViewModel> AllInterfaces { get; } = new ObservableCollection<InterfaceComponentViewModel>();
        
        private int _interfaceCount;
        public int InterfaceCount
        {
            get => _interfaceCount;
            set => SetProperty(ref _interfaceCount, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _highlightSelection = true;
        public bool HighlightSelection
        {
            get => _highlightSelection;
            set
            {
                if (!SetProperty(ref _highlightSelection, value))
                {
                    return;
                }

                if (!value)
                {
                    DebugDraw.Clear(InterfaceHighlightKey);
                }
            }
        }

        private bool _keepHighlight;
        public bool KeepHighlight
        {
            get => _keepHighlight;
            set => SetProperty(ref _keepHighlight, value);
        }

        private int _highlightDurationMs = 10000;
        public int HighlightDurationMs
        {
            get => _highlightDurationMs;
            set => SetProperty(ref _highlightDurationMs, value);
        }

        private long _lastHighlightTick;
        private string _findQuery = string.Empty;
        public string FindQuery
        {
            get => _findQuery;
            set
            {
                if (SetProperty(ref _findQuery, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _newOverrideKey;
        public string NewOverrideKey
        {
            get => _newOverrideKey;
            set => SetProperty(ref _newOverrideKey, value);
        }

        private string _overridesPath;
        public string OverridesPath
        {
            get => _overridesPath;
            set => SetProperty(ref _overridesPath, value);
        }

        public ObservableCollection<InterfaceOverrideEntry> Overrides { get; } = new ObservableCollection<InterfaceOverrideEntry>();

        public bool HasInterfaces => AllInterfaces.Count > 0;

        // ─── Dialog helpers (moved from ApiUtilities) ──────────────────────
        private string _dialogOptionText = "Continue";
        public string DialogOptionText
        {
            get => _dialogOptionText;
            set => SetProperty(ref _dialogOptionText, value);
        }
        
        public ICommand LoadInterfacesCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand UseSelectedRootCommand { get; }
        public ICommand RefreshSelectedCommand { get; }
        public ICommand CaptureOverrideCommand { get; }
        public ICommand RemoveOverrideCommand { get; }
        public ICommand ExportOverridesCommand { get; }
        public ICommand LoadOverridesCommand { get; }
        public ICommand HighlightSelectedCommand { get; }
        public ICommand ClearHighlightCommand { get; }
        public ICommand DialogSelectCommand { get; }
        public ICommand DialogContinueCommand { get; }
        public ICommand FindInterfaceCommand { get; }

        public InterfacesViewModel()
        {
            LoadInterfacesCommand = new RelayCommand(_ => LoadInterfaces());
            ClearCommand = new RelayCommand(_ =>
            {
                AllInterfaces.Clear();
                InterfaceCount = 0;
                SelectedInterface = null;
                StatusMessage = "Cleared.";
                OnPropertyChanged(nameof(HasInterfaces));
            });
            UseSelectedRootCommand = new RelayCommand(_ => UseSelectedRoot());
            RefreshSelectedCommand = new RelayCommand(_ => RefreshSelected(), _ => SelectedInterface != null);
            CaptureOverrideCommand = new RelayCommand(_ => CaptureOverride(), _ => SelectedInterface != null);
            RemoveOverrideCommand = new RelayCommand(entry => RemoveOverride(entry as InterfaceOverrideEntry), entry => entry is InterfaceOverrideEntry);
            ExportOverridesCommand = new RelayCommand(_ => ExportOverrides());
            LoadOverridesCommand = new RelayCommand(_ => LoadOverrides());
            HighlightSelectedCommand = new RelayCommand(_ => HighlightSelectionNow(), _ => SelectedInterface != null);
            ClearHighlightCommand = new RelayCommand(_ => DebugDraw.Clear(InterfaceHighlightKey));

            DialogSelectCommand = new RelayCommand(_ => RunDialog(() => Dialogs.SelectOption(DialogOptionText), "Dialogs.SelectOption"));
            DialogContinueCommand = new RelayCommand(_ => RunDialog(Dialogs.Continue, "Dialogs.Continue"));
            FindInterfaceCommand = new RelayCommand(_ => FindInterface(), _ => !string.IsNullOrWhiteSpace(FindQuery));

            _updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += OnTimerTick;

            StatusMessage = "Click Load Interfaces to scan the game UI.";
            OverridesPath = Path.GetFullPath(InterfaceOverrides.ResolveDefaultPath());
            LoadOverrides();
        }

        private void LoadInterfaces()
        {
            try
            {
                var previousSelection = SelectedInterface;
                AllInterfaces.Clear();

                int rootId = -1;
                bool getOnlyTarget = false;
                if (FocusRoot && int.TryParse(RootIdText, out int parsed))
                {
                    rootId = parsed;
                    getOnlyTarget = true;
                }

                var components = Interfaces.Scan(rootId, getOnlyTarget, textOnly: TextOnly, includeHidden: IncludeHidden);
                if (components.Count == 0)
                {
                    InterfaceCount = 0;
                    SelectedInterface = null;
                    StatusMessage = "No interfaces found.";
                    OnPropertyChanged(nameof(HasInterfaces));
                    return;
                }

                if (FilterIndex1 || FilterIndex2 || FilterIndex3)
                {
                    components = components.Where(c =>
                        (FilterIndex1 && c.Index == 1) ||
                        (FilterIndex2 && c.Index == 2) ||
                        (FilterIndex3 && c.Index == 3)).ToList();
                }

                if (components.Count == 0)
                {
                    InterfaceCount = 0;
                    SelectedInterface = null;
                    StatusMessage = "No interfaces matched the current filters.";
                    OnPropertyChanged(nameof(HasInterfaces));
                    return;
                }

                var rootNodes = new List<InterfaceComponentViewModel>();
                var parentStack = new Stack<InterfaceComponentViewModel>();

                var firstNode = new InterfaceComponentViewModel(components[0]);
                rootNodes.Add(firstNode);
                parentStack.Push(firstNode);

                for (int i = 1; i < components.Count; i++)
                {
                    var component = components[i];
                    var viewModel = new InterfaceComponentViewModel(component);

                    while (parentStack.Count > 0 && parentStack.Peek().Component.Index >= component.Index)
                    {
                        parentStack.Pop();
                    }

                    if (parentStack.Count > 0)
                    {
                        parentStack.Peek().Children.Add(viewModel);
                    }
                    else
                    {
                        rootNodes.Add(viewModel);
                    }
                    parentStack.Push(viewModel);
                }

                foreach(var root in rootNodes)
                {
                    AllInterfaces.Add(root);
                }

                InterfaceCount = components.Count;
                StatusMessage = $"Loaded {InterfaceCount} interface(s).";
                OnPropertyChanged(nameof(HasInterfaces));

                var restored = FindBestSelectionMatch(previousSelection, components);
                SelectedInterface = restored;
            }
            catch (Exception ex)
            {
                SelectedInterface = null;
                StatusMessage = $"Error: {ex.Message}";
                OnPropertyChanged(nameof(HasInterfaces));
            }
        }

        private void RunDialog(Func<bool> action, string label)
        {
            try
            {
                var ok = action();
                StatusMessage = $"{label}: {(ok ? "OK" : "Failed")}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"{label} error: {ex.Message}";
            }
        }

        private void UseSelectedRoot()
        {
            if (SelectedInterface == null)
            {
                return;
            }

            RootIdText = SelectedInterface.Id1.ToString();
            FocusRoot = true;
            LoadInterfaces();
        }

        private void RefreshSelected()
        {
            if (SelectedInterface == null)
            {
                return;
            }

            try
            {
                var refreshed = Interfaces.GetInfo(SelectedInterface.MemLoc, refreshData: true, refreshText: true);
                if (refreshed != null)
                {
                    SelectedInterface = refreshed;
                }
                else
                {
                    StatusMessage = "Selected interface is no longer available.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Refresh selected failed: {ex.Message}";
            }
        }

        private void HighlightSelectionNow()
        {
            if (!HighlightSelection || SelectedInterface == null)
            {
                return;
            }

            if (SelectedInterface.Width <= 0 || SelectedInterface.Height <= 0)
            {
                return;
            }

            var now = Environment.TickCount64;
            if (now - _lastHighlightTick < 250)
            {
                return;
            }

            _lastHighlightTick = now;
            int duration = KeepHighlight ? 0 : HighlightDurationMs;
            DebugDraw.HighlightInterface(InterfaceHighlightKey, SelectedInterface, duration, 2.0f, false, KeepHighlight);
        }

        private static bool HasSameIdentity(InterfaceComponent a, InterfaceComponent b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }

            if (a.MemLoc != 0 || b.MemLoc != 0)
            {
                return a.MemLoc == b.MemLoc;
            }

            return a.Id1 == b.Id1 && a.Id2 == b.Id2 && a.Id3 == b.Id3;
        }

        private static InterfaceComponent FindBestSelectionMatch(InterfaceComponent previousSelection, IReadOnlyList<InterfaceComponent> components)
        {
            if (previousSelection == null || components.Count == 0)
            {
                return null;
            }

            if (previousSelection.MemLoc != 0)
            {
                var byMemLoc = components.FirstOrDefault(c => c.MemLoc == previousSelection.MemLoc);
                if (byMemLoc != null)
                {
                    return byMemLoc;
                }
            }

            return components.FirstOrDefault(c =>
                c.Id1 == previousSelection.Id1 &&
                c.Id2 == previousSelection.Id2 &&
                c.Id3 == previousSelection.Id3);
        }

        private void FindInterface()
        {
            try
            {
                var query = (FindQuery ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(query))
                {
                    StatusMessage = "Enter text, id tuple (id1:id2:id3), or memloc (hex/decimal).";
                    return;
                }

                if (AllInterfaces.Count == 0)
                {
                    LoadInterfaces();
                }

                var all = EnumerateComponents(AllInterfaces).ToList();
                if (all.Count == 0)
                {
                    StatusMessage = "No interfaces are loaded to search.";
                    return;
                }

                var match = FindExactIdTuple(query, all)
                    ?? FindByMemloc(query, all)
                    ?? FindByText(query, all);

                if (match == null)
                {
                    StatusMessage = $"No interface match for \"{query}\".";
                    return;
                }

                SelectedInterface = match;
                StatusMessage = $"Matched {match.Id1}:{match.Id2}:{match.Id3} at 0x{match.MemLoc:X}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Find failed: {ex.Message}";
            }
        }

        private static IEnumerable<InterfaceComponent> EnumerateComponents(IEnumerable<InterfaceComponentViewModel> roots)
        {
            if (roots == null)
            {
                yield break;
            }

            var stack = new Stack<InterfaceComponentViewModel>(roots.Reverse());
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current?.Component != null)
                {
                    yield return current.Component;
                }

                if (current?.Children == null || current.Children.Count == 0)
                {
                    continue;
                }

                for (int i = current.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.Children[i]);
                }
            }
        }

        private static InterfaceComponent? FindExactIdTuple(string query, IReadOnlyList<InterfaceComponent> all)
        {
            var normalized = query.Replace(' ', ':').Replace(',', ':');
            var parts = normalized.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
            {
                return null;
            }

            if (!int.TryParse(parts[0], out var id1) ||
                !int.TryParse(parts[1], out var id2) ||
                !int.TryParse(parts[2], out var id3))
            {
                return null;
            }

            return all.FirstOrDefault(c => c.Id1 == id1 && c.Id2 == id2 && c.Id3 == id3);
        }

        private static InterfaceComponent? FindByMemloc(string query, IReadOnlyList<InterfaceComponent> all)
        {
            if (!TryParseUnsigned(query, out var memloc))
            {
                return null;
            }

            return all.FirstOrDefault(c => c.MemLoc == memloc || c.MemLocTop == memloc);
        }

        private static InterfaceComponent? FindByText(string query, IReadOnlyList<InterfaceComponent> all)
        {
            return all.FirstOrDefault(c =>
                Contains(c.TextItem, query) ||
                Contains(c.TextIds, query) ||
                Contains(c.FullPath, query) ||
                Contains(c.FullIdPath, query));
        }

        private static bool Contains(string? source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool TryParseUnsigned(string input, out ulong value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var trimmed = input.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return ulong.TryParse(trimmed[2..], System.Globalization.NumberStyles.HexNumber, null, out value);
            }

            return ulong.TryParse(trimmed, out value);
        }

        private void CaptureOverride()
        {
            if (SelectedInterface == null)
            {
                StatusMessage = "Select an interface component first.";
                return;
            }

            var key = (NewOverrideKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                StatusMessage = "Enter a key name before capturing (e.g., Bank.Tab.Inventory).";
                return;
            }

            var entry = new InterfaceOverrideEntry(key, new InterfaceId(SelectedInterface.Id1, SelectedInterface.Id2, SelectedInterface.Id3));
            var existing = Overrides.FirstOrDefault(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                Overrides.Remove(existing);
            }

            Overrides.Add(entry);
            StatusMessage = $"Captured {entry.Display}.";
        }

        private void RemoveOverride(InterfaceOverrideEntry entry)
        {
            if (entry == null)
                return;

            Overrides.Remove(entry);
            StatusMessage = $"Removed {entry.Key}.";
        }

        private void ExportOverrides()
        {
            var map = Overrides.ToDictionary(o => o.Key, o => o.Id, StringComparer.OrdinalIgnoreCase);
            if (InterfaceOverrides.Save(map, OverridesPath))
            {
                StatusMessage = $"Saved overrides to {OverridesPath}.";
            }
            else
            {
                StatusMessage = "Failed to save overrides.";
            }
        }

        private void LoadOverrides()
        {
            Overrides.Clear();
            var loaded = InterfaceOverrides.Load(OverridesPath);
            foreach (var kvp in InterfaceOverrides.Entries)
            {
                Overrides.Add(new InterfaceOverrideEntry(kvp.Key, kvp.Value));
            }

            if (loaded)
            {
                StatusMessage = $"Loaded overrides from {OverridesPath}.";
                return;
            }

            if (!File.Exists(OverridesPath))
            {
                var created = InterfaceOverrides.Save(new Dictionary<string, InterfaceId>(StringComparer.OrdinalIgnoreCase), OverridesPath);
                StatusMessage = created
                    ? $"Created overrides file at {OverridesPath}."
                    : "Overrides file was missing and could not be created.";
                return;
            }

            StatusMessage = "Failed to load overrides. Check the JSON format.";
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (_disposed || !_isActive || FreezeRefresh)
                return;

            LoadInterfaces();
        }

        private void UpdateTimer()
        {
            if (_disposed)
                return;

            if (!_isActive || FreezeRefresh)
            {
                if (_updateTimer.IsEnabled)
                    _updateTimer.Stop();
                return;
            }

            if (AutoRefresh)
            {
                if (!_updateTimer.IsEnabled)
                    _updateTimer.Start();
            }
            else if (_updateTimer.IsEnabled)
            {
                _updateTimer.Stop();
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        bool SetProperty<T>(ref T field, T newVal, [CallerMemberName] string propName = null)
        {
            if (!Equals(field, newVal))
            {
                field = newVal;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
                return true;
            }
            return false;
        }

        void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        #endregion

        #region IActivatableViewModel
        public void OnActivated()
        {
            if (_disposed)
                return;

            _isActive = true;
            UpdateTimer();
        }

        public void OnDeactivated()
        {
            if (_disposed || !_isActive)
                return;

            _isActive = false;
            try { _updateTimer.Stop(); } catch { /* ignore */ }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_disposed)
                return;

            OnDeactivated();
            _disposed = true;

            try
            {
                _updateTimer.Tick -= OnTimerTick;
            }
            catch { /* ignore */ }
        }
        #endregion
    }

    internal static class InterfaceTextCleaner
    {
        private static readonly Regex ColTagRegex = new Regex(@"</?col(?:=[^>]+)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var cleaned = ColTagRegex.Replace(value, string.Empty).Trim();
            return cleaned;
        }
    }
}
