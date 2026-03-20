using MESharp.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class InterfaceComponentViewModel : INotifyPropertyChanged
    {
        public InterfaceComponent Component { get; }
        public ObservableCollection<InterfaceComponentViewModel> Children { get; } = new ObservableCollection<InterfaceComponentViewModel>();
        private bool _isExpanded;

        public InterfaceComponentViewModel(InterfaceComponent component, string knownLabel = "")
        {
            Component = component;
            KnownLabel = knownLabel ?? string.Empty;
        }

        public string KnownLabel { get; }
        public bool HasKnownLabel => !string.IsNullOrWhiteSpace(KnownLabel);
        public string DisplayText => HasKnownLabel
            ? $"[{Component.Index}] {Component.Id1}:{Component.Id2}:{Component.Id3} | {KnownLabel}"
            : $"[{Component.Index}] {Component.Id1}:{Component.Id2}:{Component.Id3}";
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

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                {
                    return;
                }

                _isExpanded = value;
                OnPropertyChanged();
            }
        }


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
        private Dictionary<string, string> _knownLabels = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _expandAllCts;
        private Dictionary<string, DiffSnapshot> _previousInterfaceSnapshot = new(StringComparer.Ordinal);
        private string _previousDiffContext = string.Empty;

        private sealed class DiffSnapshot
        {
            public bool Visible { get; init; }
            public bool HasText { get; init; }
            public bool HasItem { get; init; }
            public bool Active { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
            public int ScrollY { get; init; }
        }

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

        private bool _showKnownOnly;
        public bool ShowKnownOnly
        {
            get => _showKnownOnly;
            set => SetProperty(ref _showKnownOnly, value);
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
                OnPropertyChanged(nameof(SelectedInterfaceKnownLabel));
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

        public string SelectedInterfaceKnownLabel
        {
            get
            {
                if (SelectedInterface == null)
                {
                    return string.Empty;
                }

                return TryGetKnownLabel(SelectedInterface, out var label) ? label : string.Empty;
            }
        }

        public string SelectedInterfaceTextItem => InterfaceTextCleaner.Clean(SelectedInterface?.TextItem);

        public ObservableCollection<InterfaceComponentViewModel> AllInterfaces { get; } = new ObservableCollection<InterfaceComponentViewModel>();
        
        private int _interfaceCount;
        public int InterfaceCount
        {
            get => _interfaceCount;
            set => SetProperty(ref _interfaceCount, value);
        }

        private int _knownInterfaceCount;
        public int KnownInterfaceCount
        {
            get => _knownInterfaceCount;
            set => SetProperty(ref _knownInterfaceCount, value);
        }

        private bool _isExpandAllRunning;
        public bool IsExpandAllRunning
        {
            get => _isExpandAllRunning;
            set
            {
                if (SetProperty(ref _isExpandAllRunning, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _expandAllStatus = "Idle";
        public string ExpandAllStatus
        {
            get => _expandAllStatus;
            set => SetProperty(ref _expandAllStatus, value);
        }

        private bool _showDiffOnly;
        public bool ShowDiffOnly
        {
            get => _showDiffOnly;
            set => SetProperty(ref _showDiffOnly, value);
        }

        private string _diffStatus = "Diff: baseline not set.";
        public string DiffStatus
        {
            get => _diffStatus;
            set => SetProperty(ref _diffStatus, value);
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

        private bool _filterByPoint;
        public bool FilterByPoint
        {
            get => _filterByPoint;
            set => SetProperty(ref _filterByPoint, value);
        }

        private string _pointXText = string.Empty;
        public string PointXText
        {
            get => _pointXText;
            set => SetProperty(ref _pointXText, value);
        }

        private string _pointYText = string.Empty;
        public string PointYText
        {
            get => _pointYText;
            set => SetProperty(ref _pointYText, value);
        }

        private int _pointRadius = 2;
        public int PointRadius
        {
            get => _pointRadius;
            set => SetProperty(ref _pointRadius, value);
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
        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }
        public ICommand CancelExpandAllCommand { get; }
        public ICommand ResetListDiffBaselineCommand { get; }

        public InterfacesViewModel()
        {
            LoadInterfacesCommand = new RelayCommand(_ => LoadInterfaces());
            ClearCommand = new RelayCommand(_ =>
            {
                AllInterfaces.Clear();
                InterfaceCount = 0;
                KnownInterfaceCount = 0;
                SelectedInterface = null;
                ResetListDiffBaseline();
                StatusMessage = "Cleared.";
                OnPropertyChanged(nameof(HasInterfaces));
                CommandManager.InvalidateRequerySuggested();
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
            ExpandAllCommand = new RelayCommand(_ => StartExpandAll(), _ => HasInterfaces && !IsExpandAllRunning);
            CollapseAllCommand = new RelayCommand(_ => CollapseAllFast(), _ => HasInterfaces);
            CancelExpandAllCommand = new RelayCommand(_ => CancelExpandAll(), _ => IsExpandAllRunning);
            ResetListDiffBaselineCommand = new RelayCommand(_ => ResetListDiffBaseline());

            _updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += OnTimerTick;

            StatusMessage = "Click Load Interfaces to scan the game UI.";
            OverridesPath = Path.GetFullPath(InterfaceOverrides.ResolveDefaultPath());
            LoadOverrides();
            RefreshKnownLabels();
        }

        private void LoadInterfaces()
        {
            try
            {
                var previousSelection = SelectedInterface;
                AllInterfaces.Clear();
                RefreshKnownLabels();

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
                    KnownInterfaceCount = 0;
                    SelectedInterface = null;
                    StatusMessage = "No interfaces found.";
                    OnPropertyChanged(nameof(HasInterfaces));
                    CommandManager.InvalidateRequerySuggested();
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
                    KnownInterfaceCount = 0;
                    SelectedInterface = null;
                    StatusMessage = "No interfaces matched the current filters.";
                    OnPropertyChanged(nameof(HasInterfaces));
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                if (ShowKnownOnly)
                {
                    components = components.Where(c => TryGetKnownLabel(c, out _)).ToList();
                }

                if (FilterByPoint && int.TryParse(PointXText, out var px) && int.TryParse(PointYText, out var py))
                {
                    var radius = Math.Max(0, PointRadius);
                    components = components.Where(c => IsPointInside(c, px, py, radius)).ToList();
                }

                if (components.Count == 0)
                {
                    InterfaceCount = 0;
                    KnownInterfaceCount = 0;
                    SelectedInterface = null;
                    StatusMessage = "No interfaces matched known-label filtering.";
                    OnPropertyChanged(nameof(HasInterfaces));
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                var diffContext = BuildDiffContextKey(rootId, getOnlyTarget);
                if (!string.Equals(_previousDiffContext, diffContext, StringComparison.Ordinal))
                {
                    _previousDiffContext = diffContext;
                    _previousInterfaceSnapshot = BuildDiffSnapshot(components);
                    DiffStatus = "Diff baseline reset (context changed).";
                }

                var currentSnapshot = BuildDiffSnapshot(components);
                var addedKeys = currentSnapshot.Keys.Except(_previousInterfaceSnapshot.Keys).ToHashSet(StringComparer.Ordinal);
                var removedCount = _previousInterfaceSnapshot.Keys.Except(currentSnapshot.Keys).Count();
                var changedKeys = currentSnapshot.Keys
                    .Where(_previousInterfaceSnapshot.ContainsKey)
                    .Where(key => !DiffSnapshotEquals(currentSnapshot[key], _previousInterfaceSnapshot[key]))
                    .ToHashSet(StringComparer.Ordinal);

                if (ShowDiffOnly)
                {
                    var keysToShow = addedKeys.Union(changedKeys).ToHashSet(StringComparer.Ordinal);
                    components = components
                        .Where(c => keysToShow.Contains(GetDiffKey(c)))
                        .ToList();
                }

                var rootNodes = new List<InterfaceComponentViewModel>();
                var parentStack = new Stack<InterfaceComponentViewModel>();

                if (components.Count > 0)
                {
                    var firstNode = new InterfaceComponentViewModel(components[0], ResolveKnownLabel(components[0]));
                    rootNodes.Add(firstNode);
                    parentStack.Push(firstNode);

                    for (int i = 1; i < components.Count; i++)
                    {
                        var component = components[i];
                        var viewModel = new InterfaceComponentViewModel(component, ResolveKnownLabel(component));

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
                }

                foreach(var root in rootNodes)
                {
                    AllInterfaces.Add(root);
                }

                InterfaceCount = components.Count;
                KnownInterfaceCount = components.Count(c => TryGetKnownLabel(c, out _));
                DiffStatus = $"Diff: +{addedKeys.Count} ~{changedKeys.Count} -{removedCount}";
                StatusMessage = ShowDiffOnly
                    ? $"Loaded {InterfaceCount} diff interface(s) | Known labels: {KnownInterfaceCount}."
                    : $"Loaded {InterfaceCount} interface(s) | Known labels: {KnownInterfaceCount}.";
                OnPropertyChanged(nameof(HasInterfaces));
                CommandManager.InvalidateRequerySuggested();

                var restored = FindBestSelectionMatch(previousSelection, components);
                SelectedInterface = restored;
                _previousInterfaceSnapshot = currentSnapshot;
            }
            catch (Exception ex)
            {
                SelectedInterface = null;
                StatusMessage = $"Error: {ex.Message}";
                OnPropertyChanged(nameof(HasInterfaces));
                CommandManager.InvalidateRequerySuggested();
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

        private IEnumerable<InterfaceComponentViewModel> EnumerateNodes(IEnumerable<InterfaceComponentViewModel> roots)
        {
            if (roots == null)
            {
                yield break;
            }

            var stack = new Stack<InterfaceComponentViewModel>(roots.Reverse());
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null)
                {
                    continue;
                }

                yield return current;

                if (current.Children == null || current.Children.Count == 0)
                {
                    continue;
                }

                for (int i = current.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.Children[i]);
                }
            }
        }

        private void SetTreeExpansion(bool expanded)
        {
            foreach (var node in EnumerateNodes(AllInterfaces))
            {
                node.IsExpanded = expanded;
            }
        }

        private void CollapseAllFast()
        {
            CancelExpandAll();
            foreach (var root in AllInterfaces)
            {
                root.IsExpanded = false;
            }

            ExpandAllStatus = "Collapsed root nodes.";
        }

        private void StartExpandAll()
        {
            _ = ExpandAllProgressiveAsync();
        }

        private void CancelExpandAll()
        {
            _expandAllCts?.Cancel();
        }

        private void ResetListDiffBaseline()
        {
            _previousInterfaceSnapshot.Clear();
            _previousDiffContext = string.Empty;
            DiffStatus = "Diff baseline cleared.";
        }

        private async Task ExpandAllProgressiveAsync()
        {
            if (IsExpandAllRunning)
            {
                return;
            }

            IsExpandAllRunning = true;
            _expandAllCts?.Cancel();
            _expandAllCts?.Dispose();
            _expandAllCts = new CancellationTokenSource();
            var token = _expandAllCts.Token;

            try
            {
                var total = EnumerateNodes(AllInterfaces).Count();
                if (total == 0)
                {
                    ExpandAllStatus = "No interfaces to expand.";
                    return;
                }

                int batchSize = total > 8000 ? 24 : total > 3000 ? 48 : 96;
                int delayMs = total > 8000 ? 10 : total > 3000 ? 4 : 1;

                ExpandAllStatus = $"Expanding {total} nodes (throttled)...";

                var queue = new Queue<InterfaceComponentViewModel>(AllInterfaces);
                int processed = 0;
                while (queue.Count > 0)
                {
                    token.ThrowIfCancellationRequested();

                    var node = queue.Dequeue();
                    if (node != null && !node.IsExpanded)
                    {
                        node.IsExpanded = true;
                    }

                    if (node?.Children != null)
                    {
                        foreach (var child in node.Children)
                        {
                            queue.Enqueue(child);
                        }
                    }

                    processed++;
                    if (processed % batchSize == 0)
                    {
                        ExpandAllStatus = $"Expanding... {processed}/{total}";
                        await Dispatcher.Yield(DispatcherPriority.Background);
                        await Task.Delay(delayMs, token);
                    }
                }

                ExpandAllStatus = $"Expand complete ({total} nodes).";
            }
            catch (OperationCanceledException)
            {
                ExpandAllStatus = "Expand cancelled.";
            }
            catch (Exception ex)
            {
                ExpandAllStatus = $"Expand failed: {ex.Message}";
            }
            finally
            {
                IsExpandAllRunning = false;
            }
        }

        private string BuildDiffContextKey(int rootId, bool getOnlyTarget)
        {
            return string.Join("|",
                $"root={rootId}",
                $"target={getOnlyTarget}",
                $"hidden={IncludeHidden}",
                $"textOnly={TextOnly}",
                $"f1={FilterIndex1}",
                $"f2={FilterIndex2}",
                $"f3={FilterIndex3}",
                $"knownOnly={ShowKnownOnly}");
        }

        private static string GetDiffKey(InterfaceComponent c)
        {
            return $"{c.Id1}:{c.Id2}:{c.Id3}:{c.Index}";
        }

        private static Dictionary<string, DiffSnapshot> BuildDiffSnapshot(IEnumerable<InterfaceComponent> components)
        {
            var map = new Dictionary<string, DiffSnapshot>(StringComparer.Ordinal);
            foreach (var c in components)
            {
                map[GetDiffKey(c)] = new DiffSnapshot
                {
                    Visible = !c.IsNotVisible,
                    HasText = !string.IsNullOrWhiteSpace(c.TextIds) || !string.IsNullOrWhiteSpace(c.TextItem),
                    HasItem = c.ItemId > 0 || c.ItemId2 > 0 || c.ItemStack > 0,
                    Active = c.Op != 0 || c.IsHovered,
                    X = c.X,
                    Y = c.Y,
                    Width = c.Width,
                    Height = c.Height,
                    ScrollY = c.ScrollY
                };
            }

            return map;
        }

        private static bool DiffSnapshotEquals(DiffSnapshot a, DiffSnapshot b)
        {
            return a.Visible == b.Visible &&
                   a.HasText == b.HasText &&
                   a.HasItem == b.HasItem &&
                   a.Active == b.Active &&
                   a.X == b.X &&
                   a.Y == b.Y &&
                   a.Width == b.Width &&
                   a.Height == b.Height &&
                   a.ScrollY == b.ScrollY;
        }

        private void RefreshKnownLabels()
        {
            _knownLabels = InterfaceKnownLabels.BuildLabelMap(InterfaceOverrides.Entries);
        }

        private bool TryGetKnownLabel(InterfaceComponent component, out string label)
        {
            label = string.Empty;
            if (component == null || _knownLabels == null || _knownLabels.Count == 0)
            {
                return false;
            }

            return InterfaceKnownLabels.TryGetLabel(component.Id1, component.Id2, component.Id3, _knownLabels, out label);
        }

        private string ResolveKnownLabel(InterfaceComponent component)
        {
            return TryGetKnownLabel(component, out var label) ? label : string.Empty;
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

        private static bool IsPointInside(InterfaceComponent c, int x, int y, int radius)
        {
            var left = c.X - radius;
            var top = c.Y - radius;
            var right = c.X + c.Width + radius;
            var bottom = c.Y + c.Height + radius;
            return x >= left && x <= right && y >= top && y <= bottom;
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
            RefreshKnownLabels();
            OnPropertyChanged(nameof(SelectedInterfaceKnownLabel));
            StatusMessage = $"Captured {entry.Display}.";
        }

        private void RemoveOverride(InterfaceOverrideEntry entry)
        {
            if (entry == null)
                return;

            Overrides.Remove(entry);
            RefreshKnownLabels();
            OnPropertyChanged(nameof(SelectedInterfaceKnownLabel));
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

            RefreshKnownLabels();
            OnPropertyChanged(nameof(SelectedInterfaceKnownLabel));

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
