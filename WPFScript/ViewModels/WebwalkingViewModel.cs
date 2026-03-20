using MESharp.API;
using MESharp.Commands;
using MESharp.Models;
using MESharp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    public sealed class WebwalkingViewModel : BaseViewModel, IDisposable, IActivatableViewModel
    {
        private readonly DispatcherTimer _refreshTimer;
        private readonly NotifyCollectionChangedEventHandler _currentRouteChangedHandler;
        private readonly Random _random = new();
        private CancellationTokenSource? _runCts;
        private bool _isActive;
        private bool _disposed;
        private bool _isDirty;
        private bool _isRunning;

        private string _lastStatus = "Ready.";
        private string _runStatus = "Idle";
        private string _currentTile = "--";

        private RouteDefinition? _selectedRoute;
        private RouteWaypoint? _selectedWaypoint;

        private string _searchText = string.Empty;
        private string _routeName = "new.route";
        private string _routeDescription = string.Empty;
        private string _routeCategory = "custom";
        private string _routeTags = string.Empty;
        private bool _routeEnabled = true;
        private string _renameText = string.Empty;

        private string _targetX = string.Empty;
        private string _targetY = string.Empty;
        private string _targetZ = "0";

        private string _wpLabel = string.Empty;
        private int _wpX;
        private int _wpY;
        private int _wpZ;
        private int _wpAreaRadius = 1;
        private int _wpArrivalDistance = 2;
        private int _wpTimeoutMs = 9000;
        private int _wpJitterTiles = 1;
        private bool _wpChainWhileMoving = true;
        private bool _wpIsTransition;
        private string _wpTransitionIds = string.Empty;

        public ObservableCollection<string> ActivityLog { get; } = new();
        public ObservableCollection<RouteDefinition> SavedRoutes { get; } = new();
        public ObservableCollection<RouteDefinition> FilteredRoutes { get; } = new();
        public ObservableCollection<RouteWaypoint> CurrentRoute { get; } = new();

        public string LastStatus { get => _lastStatus; set => SetProperty(ref _lastStatus, value); }
        public string RunStatus { get => _runStatus; set => SetProperty(ref _runStatus, value); }
        public string CurrentTile { get => _currentTile; set => SetProperty(ref _currentTile, value); }
        public bool IsRunning { get => _isRunning; set { SetProperty(ref _isRunning, value); RefreshCommandStates(); } }
        public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyRouteFilter();
                }
            }
        }

        public RouteDefinition? SelectedRoute
        {
            get => _selectedRoute;
            set
            {
                if (SetProperty(ref _selectedRoute, value))
                {
                    RenameText = value?.Name ?? string.Empty;
                    RefreshCommandStates();
                }
            }
        }

        public RouteWaypoint? SelectedWaypoint
        {
            get => _selectedWaypoint;
            set
            {
                if (SetProperty(ref _selectedWaypoint, value))
                {
                    LoadWaypointEditor(value);
                    RefreshCommandStates();
                }
            }
        }

        public string RouteName { get => _routeName; set { SetProperty(ref _routeName, value); RefreshCommandStates(); } }
        public string RouteDescription { get => _routeDescription; set => SetProperty(ref _routeDescription, value); }
        public string RouteCategory { get => _routeCategory; set => SetProperty(ref _routeCategory, value); }
        public string RouteTags { get => _routeTags; set => SetProperty(ref _routeTags, value); }
        public bool RouteEnabled { get => _routeEnabled; set => SetProperty(ref _routeEnabled, value); }
        public string RenameText { get => _renameText; set => SetProperty(ref _renameText, value); }

        public string TargetX { get => _targetX; set => SetProperty(ref _targetX, value); }
        public string TargetY { get => _targetY; set => SetProperty(ref _targetY, value); }
        public string TargetZ { get => _targetZ; set => SetProperty(ref _targetZ, value); }

        public string WaypointLabel { get => _wpLabel; set => SetProperty(ref _wpLabel, value); }
        public int WaypointX { get => _wpX; set => SetProperty(ref _wpX, value); }
        public int WaypointY { get => _wpY; set => SetProperty(ref _wpY, value); }
        public int WaypointZ { get => _wpZ; set => SetProperty(ref _wpZ, value); }
        public int WaypointAreaRadius { get => _wpAreaRadius; set => SetProperty(ref _wpAreaRadius, value); }
        public int WaypointArrivalDistance { get => _wpArrivalDistance; set => SetProperty(ref _wpArrivalDistance, value); }
        public int WaypointTimeoutMs { get => _wpTimeoutMs; set => SetProperty(ref _wpTimeoutMs, value); }
        public int WaypointJitterTiles { get => _wpJitterTiles; set => SetProperty(ref _wpJitterTiles, value); }
        public bool WaypointChainWhileMoving { get => _wpChainWhileMoving; set => SetProperty(ref _wpChainWhileMoving, value); }
        public bool WaypointIsTransition { get => _wpIsTransition; set => SetProperty(ref _wpIsTransition, value); }
        public string WaypointTransitionIds { get => _wpTransitionIds; set => SetProperty(ref _wpTransitionIds, value); }

        public ICommand NewRouteCommand { get; }
        public ICommand SaveRouteCommand { get; }
        public ICommand LoadRouteCommand { get; }
        public ICommand DuplicateRouteCommand { get; }
        public ICommand RenameRouteCommand { get; }
        public ICommand DeleteRouteCommand { get; }
        public ICommand MoveRouteUpCommand { get; }
        public ICommand MoveRouteDownCommand { get; }
        public ICommand RefreshRouteListCommand { get; }

        public ICommand AddCurrentWaypointCommand { get; }
        public ICommand AddTargetWaypointCommand { get; }
        public ICommand InsertWaypointAboveCommand { get; }
        public ICommand InsertWaypointBelowCommand { get; }
        public ICommand RemoveWaypointCommand { get; }
        public ICommand MoveWaypointUpCommand { get; }
        public ICommand MoveWaypointDownCommand { get; }
        public ICommand ApplyWaypointEditsCommand { get; }
        public ICommand ClearCurrentRouteCommand { get; }
        public ICommand UseCurrentTileAsTargetCommand { get; }
        public ICommand UseCurrentTileForWaypointCommand { get; }
        public ICommand CopyTargetToWaypointCommand { get; }
        public ICommand ResetWaypointDefaultsCommand { get; }

        public ICommand RunCurrentRouteCommand { get; }
        public ICommand RunSelectedRouteCommand { get; }
        public ICommand StopRouteCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public WebwalkingViewModel()
        {
            NewRouteCommand = new RelayCommand(_ => NewRoute());
            SaveRouteCommand = new RelayCommand(_ => SaveCurrentRoute(), _ => CanSaveCurrentRoute());
            LoadRouteCommand = new RelayCommand(_ => LoadSelectedRoute(), _ => SelectedRoute != null && !IsRunning);
            DuplicateRouteCommand = new RelayCommand(_ => DuplicateSelectedRoute(), _ => SelectedRoute != null && !IsRunning);
            RenameRouteCommand = new RelayCommand(_ => RenameSelectedRoute(), _ => SelectedRoute != null && !string.IsNullOrWhiteSpace(RenameText) && !IsRunning);
            DeleteRouteCommand = new RelayCommand(_ => DeleteSelectedRoute(), _ => SelectedRoute != null && !IsRunning);
            MoveRouteUpCommand = new RelayCommand(_ => MoveSelectedRoute(-1), _ => CanMoveSelectedRoute(-1) && !IsRunning);
            MoveRouteDownCommand = new RelayCommand(_ => MoveSelectedRoute(1), _ => CanMoveSelectedRoute(1) && !IsRunning);
            RefreshRouteListCommand = new RelayCommand(_ => RefreshRouteList());

            AddCurrentWaypointCommand = new RelayCommand(_ => AddWaypointFromCurrent(), _ => !IsRunning);
            AddTargetWaypointCommand = new RelayCommand(_ => AddWaypointFromTarget(), _ => !IsRunning);
            InsertWaypointAboveCommand = new RelayCommand(_ => InsertWaypointAbove(), _ => SelectedWaypoint != null && !IsRunning);
            InsertWaypointBelowCommand = new RelayCommand(_ => InsertWaypointBelow(), _ => SelectedWaypoint != null && !IsRunning);
            RemoveWaypointCommand = new RelayCommand(_ => RemoveSelectedWaypoint(), _ => SelectedWaypoint != null && !IsRunning);
            MoveWaypointUpCommand = new RelayCommand(_ => MoveSelectedWaypoint(-1), _ => CanMoveSelectedWaypoint(-1) && !IsRunning);
            MoveWaypointDownCommand = new RelayCommand(_ => MoveSelectedWaypoint(1), _ => CanMoveSelectedWaypoint(1) && !IsRunning);
            ApplyWaypointEditsCommand = new RelayCommand(_ => ApplyWaypointEdits(), _ => SelectedWaypoint != null && !IsRunning);
            ClearCurrentRouteCommand = new RelayCommand(_ => ClearCurrentRoute(), _ => CurrentRoute.Any() && !IsRunning);
            UseCurrentTileAsTargetCommand = new RelayCommand(_ => UseCurrentTileAsTarget(), _ => !IsRunning);
            UseCurrentTileForWaypointCommand = new RelayCommand(_ => UseCurrentTileForWaypoint(), _ => !IsRunning);
            CopyTargetToWaypointCommand = new RelayCommand(_ => CopyTargetToWaypoint(), _ => !IsRunning);
            ResetWaypointDefaultsCommand = new RelayCommand(_ => ResetWaypointDefaults(), _ => !IsRunning);

            RunCurrentRouteCommand = new RelayCommand(_ => RunCurrentRoute(), _ => CurrentRoute.Any() && !IsRunning);
            RunSelectedRouteCommand = new RelayCommand(_ => RunSelectedRoute(), _ => SelectedRoute != null && !IsRunning);
            StopRouteCommand = new RelayCommand(_ => StopRun(), _ => IsRunning);
            ShowHelpCommand = new RelayCommand(_ => ShowHelpWindow());

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(750)
            };
            _refreshTimer.Tick += OnRefreshTick;

            _currentRouteChangedHandler = (_, __) =>
            {
                IsDirty = true;
                RefreshCommandStates();
            };
            CurrentRoute.CollectionChanged += _currentRouteChangedHandler;

            RefreshRouteList();
            AddLog("Webwalking tooling ready.");
        }

        private void RefreshRouteList()
        {
            var currentSelectionId = SelectedRoute?.Id;
            SavedRoutes.Clear();
            foreach (var route in RouteStore.Load())
            {
                SavedRoutes.Add(route);
            }

            if (!string.IsNullOrWhiteSpace(RouteStore.LastError))
            {
                LastStatus = RouteStore.LastError;
                AddLog(LastStatus);
            }

            ApplyRouteFilter();
            if (!string.IsNullOrWhiteSpace(currentSelectionId))
            {
                SelectedRoute = FilteredRoutes.FirstOrDefault(r => string.Equals(r.Id, currentSelectionId, StringComparison.OrdinalIgnoreCase));
            }
            else if (FilteredRoutes.Count > 0)
            {
                SelectedRoute = FilteredRoutes[0];
            }

            LastStatus = $"Loaded {SavedRoutes.Count} routes.";
            RefreshCommandStates();
        }

        private void ApplyRouteFilter()
        {
            var search = (SearchText ?? string.Empty).Trim();

            FilteredRoutes.Clear();
            foreach (var route in SavedRoutes)
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    FilteredRoutes.Add(route);
                    continue;
                }

                var tagMatch = route.Tags?.Any(t => t.Contains(search, StringComparison.OrdinalIgnoreCase)) == true;
                if (route.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    route.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    tagMatch)
                {
                    FilteredRoutes.Add(route);
                }
            }
        }

        private void NewRoute()
        {
            ClearCurrentRoute();
            RouteName = "new.route";
            RouteDescription = string.Empty;
            RouteCategory = "custom";
            RouteTags = string.Empty;
            RouteEnabled = true;
            SelectedRoute = null;
            RenameText = string.Empty;
            IsDirty = false;
            LastStatus = "Started a new route draft.";
        }

        private bool CanSaveCurrentRoute()
        {
            return !IsRunning &&
                   !string.IsNullOrWhiteSpace(RouteName) &&
                   CurrentRoute.Count > 0;
        }

        private void SaveCurrentRoute()
        {
            if (!CanSaveCurrentRoute())
            {
                LastStatus = "Route save requires a name and at least one waypoint.";
                return;
            }

            var normalizedName = RouteName.Trim();
            var existing = FindExistingRouteForSave(normalizedName);

            var route = new RouteDefinition
            {
                SchemaVersion = RouteDefinition.CurrentSchemaVersion,
                Id = existing?.Id ?? BuildRouteId(normalizedName),
                Name = normalizedName,
                Description = (RouteDescription ?? string.Empty).Trim(),
                Category = string.IsNullOrWhiteSpace(RouteCategory) ? "custom" : RouteCategory.Trim(),
                IsEnabled = RouteEnabled,
                Tags = ParseTags(RouteTags),
                CreatedAt = existing == null || existing.CreatedAt == default ? DateTime.UtcNow : existing.CreatedAt,
                SavedAt = DateTime.UtcNow,
                Waypoints = CurrentRoute.Select(CloneWaypoint).ToList()
            };
            route.Normalize();

            if (existing is null)
            {
                SavedRoutes.Add(route);
            }
            else
            {
                var index = SavedRoutes.IndexOf(existing);
                SavedRoutes[index] = route;
            }

            if (!PersistRoutesWithFeedback())
            {
                return;
            }
            RefreshRouteList();
            SelectedRoute = SavedRoutes.FirstOrDefault(r => string.Equals(r.Id, route.Id, StringComparison.OrdinalIgnoreCase));
            RenameText = route.Name;
            IsDirty = false;
            LastStatus = $"Saved route '{route.Name}' ({route.Waypoints.Count} waypoints).";
            AddLog(LastStatus);
        }

        private RouteDefinition? FindExistingRouteForSave(string normalizedName)
        {
            if (SelectedRoute != null)
            {
                return SavedRoutes.FirstOrDefault(r => string.Equals(r.Id, SelectedRoute.Id, StringComparison.OrdinalIgnoreCase))
                    ?? SavedRoutes.FirstOrDefault(r => string.Equals(r.Name, SelectedRoute.Name, StringComparison.OrdinalIgnoreCase));
            }

            return SavedRoutes.FirstOrDefault(r => string.Equals(r.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildRouteId(string name)
        {
            var safe = (name ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_');
            return string.IsNullOrWhiteSpace(safe) ? $"route_{Guid.NewGuid():N}" : safe;
        }

        private static List<string> ParseTags(string tagsText)
        {
            if (string.IsNullOrWhiteSpace(tagsText))
            {
                return new List<string>();
            }

            return tagsText
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void LoadSelectedRoute()
        {
            var route = SelectedRoute;
            if (route == null)
            {
                LastStatus = "Select a route to load.";
                return;
            }

            CurrentRoute.Clear();
            foreach (var waypoint in route.Waypoints)
            {
                CurrentRoute.Add(CloneWaypoint(waypoint));
            }

            RouteName = route.Name;
            RouteDescription = route.Description;
            RouteCategory = route.Category;
            RouteEnabled = route.IsEnabled;
            RouteTags = string.Join(", ", route.Tags ?? new List<string>());
            RenameText = route.Name;
            IsDirty = false;
            LastStatus = $"Loaded route '{route.Name}'.";
            AddLog(LastStatus);
            RefreshCommandStates();
        }

        private void DuplicateSelectedRoute()
        {
            var source = SelectedRoute;
            if (source == null)
            {
                return;
            }

            var duplicate = CloneRoute(source);
            duplicate.Id = $"{source.Id}_copy_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            duplicate.Name = BuildUniqueDuplicateName(source.Name);
            duplicate.CreatedAt = DateTime.UtcNow;
            duplicate.SavedAt = DateTime.UtcNow;

            SavedRoutes.Add(duplicate);
            if (!PersistRoutesWithFeedback())
            {
                return;
            }
            RefreshRouteList();
            SelectedRoute = SavedRoutes.FirstOrDefault(r => string.Equals(r.Id, duplicate.Id, StringComparison.OrdinalIgnoreCase));
            LastStatus = $"Duplicated route as '{duplicate.Name}'.";
            AddLog(LastStatus);
        }

        private string BuildUniqueDuplicateName(string baseName)
        {
            var candidate = $"{baseName} (copy)";
            var suffix = 2;
            while (SavedRoutes.Any(r => string.Equals(r.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = $"{baseName} (copy {suffix})";
                suffix++;
            }

            return candidate;
        }

        private void RenameSelectedRoute()
        {
            var route = SelectedRoute;
            var newName = (RenameText ?? string.Empty).Trim();
            if (route == null || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            var duplicateName = SavedRoutes.Any(r => !ReferenceEquals(r, route) && string.Equals(r.Name, newName, StringComparison.OrdinalIgnoreCase));
            if (duplicateName)
            {
                LastStatus = $"A route named '{newName}' already exists.";
                return;
            }

            route.Name = newName;
            route.SavedAt = DateTime.UtcNow;
            route.Normalize();
            if (!PersistRoutesWithFeedback())
            {
                return;
            }
            RefreshRouteList();
            SelectedRoute = SavedRoutes.FirstOrDefault(r => string.Equals(r.Id, route.Id, StringComparison.OrdinalIgnoreCase));
            LastStatus = $"Renamed route to '{newName}'.";
            AddLog(LastStatus);
        }

        private void DeleteSelectedRoute()
        {
            var route = SelectedRoute;
            if (route == null)
            {
                return;
            }

            SavedRoutes.Remove(route);
            if (!PersistRoutesWithFeedback())
            {
                return;
            }
            RefreshRouteList();
            LastStatus = $"Deleted route '{route.Name}'.";
            AddLog(LastStatus);
        }

        private bool CanMoveSelectedRoute(int direction)
        {
            if (SelectedRoute == null)
            {
                return false;
            }

            var index = SavedRoutes.IndexOf(SelectedRoute);
            if (index < 0)
            {
                return false;
            }

            var target = index + direction;
            return target >= 0 && target < SavedRoutes.Count;
        }

        private void MoveSelectedRoute(int direction)
        {
            if (!CanMoveSelectedRoute(direction) || SelectedRoute == null)
            {
                return;
            }

            var index = SavedRoutes.IndexOf(SelectedRoute);
            var target = index + direction;
            SavedRoutes.Move(index, target);
            if (!PersistRoutesWithFeedback())
            {
                return;
            }
            ApplyRouteFilter();
            SelectedRoute = SavedRoutes[target];
            LastStatus = $"Moved route to row {target + 1}.";
        }

        private void AddWaypointFromCurrent()
        {
            try
            {
                var tile = LocalPlayer.GetTilePosition();
                var waypoint = BuildWaypoint(tile.x, tile.y, tile.z);
                CurrentRoute.Add(waypoint);
                SelectedWaypoint = waypoint;
                LastStatus = $"Added waypoint {waypoint}.";
            }
            catch (Exception ex)
            {
                LastStatus = $"Failed to read current tile: {ex.Message}";
            }
        }

        private void AddWaypointFromTarget()
        {
            if (!TryParseTarget(out var x, out var y, out var z))
            {
                LastStatus = "Enter valid X/Y target coordinates.";
                return;
            }

            var waypoint = BuildWaypoint(x, y, z);
            CurrentRoute.Add(waypoint);
            SelectedWaypoint = waypoint;
            LastStatus = $"Added waypoint {waypoint}.";
        }

        private RouteWaypoint BuildWaypoint(int x, int y, int z)
        {
            var waypoint = new RouteWaypoint
            {
                Id = Guid.NewGuid().ToString("N"),
                Label = string.IsNullOrWhiteSpace(WaypointLabel) ? string.Empty : WaypointLabel.Trim(),
                X = x,
                Y = y,
                Z = z,
                AreaRadius = WaypointAreaRadius,
                ArrivalDistance = WaypointArrivalDistance,
                TimeoutMs = WaypointTimeoutMs,
                JitterTiles = WaypointJitterTiles,
                ChainWhileMoving = WaypointChainWhileMoving,
                IsTransition = WaypointIsTransition,
                TransitionObjectIds = ParseTransitionIds(WaypointTransitionIds)
            };
            waypoint.Normalize();
            return waypoint;
        }

        private void InsertWaypointAbove()
        {
            if (SelectedWaypoint == null)
            {
                return;
            }

            var index = CurrentRoute.IndexOf(SelectedWaypoint);
            if (index < 0)
            {
                return;
            }

            var copy = CloneWaypoint(SelectedWaypoint);
            copy.Id = Guid.NewGuid().ToString("N");
            CurrentRoute.Insert(index, copy);
            SelectedWaypoint = copy;
            LastStatus = $"Inserted waypoint above row {index + 1}.";
        }

        private void InsertWaypointBelow()
        {
            if (SelectedWaypoint == null)
            {
                return;
            }

            var index = CurrentRoute.IndexOf(SelectedWaypoint);
            if (index < 0)
            {
                return;
            }

            var copy = CloneWaypoint(SelectedWaypoint);
            copy.Id = Guid.NewGuid().ToString("N");
            var insertIndex = Math.Min(CurrentRoute.Count, index + 1);
            CurrentRoute.Insert(insertIndex, copy);
            SelectedWaypoint = copy;
            LastStatus = $"Inserted waypoint below row {index + 1}.";
        }

        private void RemoveSelectedWaypoint()
        {
            if (SelectedWaypoint == null)
            {
                return;
            }

            var removed = SelectedWaypoint;
            CurrentRoute.Remove(removed);
            SelectedWaypoint = CurrentRoute.FirstOrDefault();
            LastStatus = $"Removed waypoint {removed}.";
        }

        private bool CanMoveSelectedWaypoint(int direction)
        {
            if (SelectedWaypoint == null)
            {
                return false;
            }

            var index = CurrentRoute.IndexOf(SelectedWaypoint);
            if (index < 0)
            {
                return false;
            }

            var target = index + direction;
            return target >= 0 && target < CurrentRoute.Count;
        }

        private void MoveSelectedWaypoint(int direction)
        {
            if (!CanMoveSelectedWaypoint(direction) || SelectedWaypoint == null)
            {
                return;
            }

            var index = CurrentRoute.IndexOf(SelectedWaypoint);
            var target = index + direction;
            CurrentRoute.Move(index, target);
            SelectedWaypoint = CurrentRoute[target];
            LastStatus = $"Moved waypoint to row {target + 1}.";
        }

        private void ApplyWaypointEdits()
        {
            if (SelectedWaypoint == null)
            {
                return;
            }

            SelectedWaypoint.Label = (WaypointLabel ?? string.Empty).Trim();
            SelectedWaypoint.X = WaypointX;
            SelectedWaypoint.Y = WaypointY;
            SelectedWaypoint.Z = WaypointZ;
            SelectedWaypoint.AreaRadius = WaypointAreaRadius;
            SelectedWaypoint.ArrivalDistance = WaypointArrivalDistance;
            SelectedWaypoint.TimeoutMs = WaypointTimeoutMs;
            SelectedWaypoint.JitterTiles = WaypointJitterTiles;
            SelectedWaypoint.ChainWhileMoving = WaypointChainWhileMoving;
            SelectedWaypoint.IsTransition = WaypointIsTransition;
            SelectedWaypoint.TransitionObjectIds = ParseTransitionIds(WaypointTransitionIds);
            SelectedWaypoint.Normalize();

            var index = CurrentRoute.IndexOf(SelectedWaypoint);
            if (index >= 0)
            {
                CurrentRoute[index] = CloneWaypoint(SelectedWaypoint);
                SelectedWaypoint = CurrentRoute[index];
            }

            IsDirty = true;
            LastStatus = "Applied waypoint edits.";
        }

        private static List<int> ParseTransitionIds(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<int>();
            }

            return input
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => int.TryParse(part, out _))
                .Select(int.Parse)
                .Distinct()
                .ToList();
        }

        private void LoadWaypointEditor(RouteWaypoint? waypoint)
        {
            if (waypoint == null)
            {
                ClearWaypointEditor();
                return;
            }

            WaypointLabel = waypoint.Label;
            WaypointX = waypoint.X;
            WaypointY = waypoint.Y;
            WaypointZ = waypoint.Z;
            WaypointAreaRadius = waypoint.AreaRadius;
            WaypointArrivalDistance = waypoint.ArrivalDistance;
            WaypointTimeoutMs = waypoint.TimeoutMs;
            WaypointJitterTiles = waypoint.JitterTiles;
            WaypointChainWhileMoving = waypoint.ChainWhileMoving;
            WaypointIsTransition = waypoint.IsTransition;
            WaypointTransitionIds = string.Join(",", waypoint.TransitionObjectIds ?? new List<int>());
        }

        private void ClearWaypointEditor()
        {
            WaypointLabel = string.Empty;
            WaypointX = 0;
            WaypointY = 0;
            WaypointZ = 0;
            ResetWaypointDefaults();
        }

        private void ResetWaypointDefaults()
        {
            WaypointAreaRadius = 1;
            WaypointArrivalDistance = 2;
            WaypointTimeoutMs = 9000;
            WaypointJitterTiles = 1;
            WaypointChainWhileMoving = true;
            WaypointIsTransition = false;
            WaypointTransitionIds = string.Empty;
        }

        private void ClearCurrentRoute()
        {
            CurrentRoute.Clear();
            SelectedWaypoint = null;
            IsDirty = true;
            LastStatus = "Cleared current route draft.";
            RefreshCommandStates();
        }

        private void RunCurrentRoute()
        {
            if (!CurrentRoute.Any())
            {
                LastStatus = "Current route draft is empty.";
                return;
            }

            _ = RunRouteAsync(RouteName, CurrentRoute.Select(CloneWaypoint).ToList());
        }

        private void RunSelectedRoute()
        {
            var route = SelectedRoute;
            if (route == null)
            {
                LastStatus = "Select a route to run.";
                return;
            }

            _ = RunRouteAsync(route.Name, route.Waypoints.Select(CloneWaypoint).ToList());
        }

        private async Task RunRouteAsync(string routeName, IReadOnlyList<RouteWaypoint> waypoints)
        {
            if (_disposed || IsRunning)
            {
                return;
            }

            _runCts?.Cancel();
            _runCts?.Dispose();
            _runCts = new CancellationTokenSource();
            var ct = _runCts.Token;

            IsRunning = true;
            RunStatus = $"Running {routeName}";
            AddLog($"Route run started: {routeName} ({waypoints.Count} waypoints)");

            try
            {
                for (var i = 0; i < waypoints.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var waypoint = waypoints[i];
                    waypoint.Normalize();
                    RunStatus = $"WP {i + 1}/{waypoints.Count}: {waypoint}";

                    var dispatched = waypoint.IsTransition
                        ? TryExecuteTransitionWaypoint(waypoint)
                        : TryExecuteWalkWaypoint(waypoint);

                    if (!dispatched)
                    {
                        LastStatus = $"Route '{routeName}' failed to dispatch waypoint {i + 1}.";
                        AddLog(LastStatus);
                        return;
                    }

                    var reached = await WaitForWaypointAsync(waypoint, ct);
                    if (!reached)
                    {
                        LastStatus = $"Route '{routeName}' timed out at waypoint {i + 1}.";
                        AddLog(LastStatus);
                        return;
                    }

                    await Task.Delay(100, ct);
                }

                LastStatus = $"Route '{routeName}' completed ({waypoints.Count} waypoints).";
                AddLog(LastStatus);
            }
            catch (OperationCanceledException)
            {
                LastStatus = $"Route '{routeName}' cancelled.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"Route '{routeName}' error: {ex.Message}";
                AddLog(LastStatus);
            }
            finally
            {
                IsRunning = false;
                RunStatus = "Idle";
                _runCts?.Dispose();
                _runCts = null;
            }
        }

        private bool TryExecuteWalkWaypoint(RouteWaypoint waypoint)
        {
            var clickX = waypoint.X;
            var clickY = waypoint.Y;

            if (waypoint.AreaRadius > 0)
            {
                clickX += _random.Next(-waypoint.AreaRadius, waypoint.AreaRadius + 1);
                clickY += _random.Next(-waypoint.AreaRadius, waypoint.AreaRadius + 1);
            }

            return Traversal.ClickTo(clickX, clickY, waypoint.Z, waypoint.JitterTiles);
        }

        private bool TryExecuteTransitionWaypoint(RouteWaypoint waypoint)
        {
            if (waypoint.TransitionObjectIds?.Count > 0)
            {
                var candidate = Objects.GetAll()
                    .Where(o => (o.Type == (int)Objects.ObjectKind.Object || o.Type == (int)Objects.ObjectKind.Object12)
                                && waypoint.TransitionObjectIds.Contains(o.Id)
                                && o.Distance <= 12)
                    .OrderBy(o => o.Distance)
                    .FirstOrDefault();

                if (candidate != null)
                {
                    return Objects.DoActionByIds(new[] { candidate.Id }, 1, Objects.Offsets.GeneralRoute0, 12);
                }
            }

            return TryExecuteWalkWaypoint(waypoint);
        }

        private async Task<bool> WaitForWaypointAsync(RouteWaypoint waypoint, CancellationToken ct)
        {
            var start = Environment.TickCount64;
            var timeout = Math.Max(1000, waypoint.TimeoutMs);

            while (!ct.IsCancellationRequested && Environment.TickCount64 - start <= timeout)
            {
                var tile = LocalPlayer.GetTilePosition();
                var inArea = waypoint.IsWithinArea(tile.x, tile.y, tile.z);
                var dx = Math.Abs(waypoint.X - tile.x);
                var dy = Math.Abs(waypoint.Y - tile.y);
                var withinDistance = Math.Max(dx, dy) <= Math.Max(0, waypoint.ArrivalDistance);

                if (inArea && withinDistance)
                {
                    return true;
                }

                if (waypoint.ChainWhileMoving && inArea && LocalPlayer.IsMoving())
                {
                    return true;
                }

                await Task.Delay(90, ct);
            }

            return false;
        }

        private void StopRun()
        {
            _runCts?.Cancel();
            RunStatus = "Stopping...";
        }

        private void UpdateCurrentTile()
        {
            if (TryGetCurrentTile(out var x, out var y, out var z))
            {
                CurrentTile = $"{x}, {y}, {z}";
            }
            else
            {
                CurrentTile = "--";
            }
        }

        private bool TryGetCurrentTile(out int x, out int y, out int z)
        {
            x = y = z = 0;
            try
            {
                var tile = LocalPlayer.GetTilePosition();
                x = tile.x;
                y = tile.y;
                z = tile.z;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void UseCurrentTileAsTarget()
        {
            if (!TryGetCurrentTile(out var x, out var y, out var z))
            {
                LastStatus = "Failed to read current tile.";
                return;
            }

            TargetX = x.ToString();
            TargetY = y.ToString();
            TargetZ = z.ToString();
            LastStatus = "Copied current tile into target inputs.";
        }

        private void UseCurrentTileForWaypoint()
        {
            if (!TryGetCurrentTile(out var x, out var y, out var z))
            {
                LastStatus = "Failed to read current tile.";
                return;
            }

            WaypointX = x;
            WaypointY = y;
            WaypointZ = z;
            LastStatus = "Copied current tile into waypoint editor.";
        }

        private void CopyTargetToWaypoint()
        {
            if (!TryParseTarget(out var x, out var y, out var z))
            {
                LastStatus = "Enter valid target coordinates first.";
                return;
            }

            WaypointX = x;
            WaypointY = y;
            WaypointZ = z;
            LastStatus = "Copied target inputs into waypoint editor.";
        }

        private bool TryParseTarget(out int x, out int y, out int z)
        {
            x = y = z = 0;
            if (!int.TryParse(TargetX, out x))
            {
                return false;
            }

            if (!int.TryParse(TargetY, out y))
            {
                return false;
            }

            if (!int.TryParse(TargetZ, out z))
            {
                z = 0;
            }

            return true;
        }

        private static RouteDefinition CloneRoute(RouteDefinition source)
        {
            return new RouteDefinition
            {
                SchemaVersion = source.SchemaVersion,
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Category = source.Category,
                IsEnabled = source.IsEnabled,
                Tags = source.Tags?.ToList() ?? new List<string>(),
                Waypoints = source.Waypoints?.Select(CloneWaypoint).ToList() ?? new List<RouteWaypoint>(),
                CreatedAt = source.CreatedAt,
                SavedAt = source.SavedAt
            };
        }

        private static RouteWaypoint CloneWaypoint(RouteWaypoint source)
        {
            var clone = new RouteWaypoint
            {
                Id = source.Id,
                Label = source.Label,
                X = source.X,
                Y = source.Y,
                Z = source.Z,
                AreaRadius = source.AreaRadius,
                ArrivalDistance = source.ArrivalDistance,
                TimeoutMs = source.TimeoutMs,
                JitterTiles = source.JitterTiles,
                ChainWhileMoving = source.ChainWhileMoving,
                IsTransition = source.IsTransition,
                TransitionObjectIds = source.TransitionObjectIds?.ToList() ?? new List<int>()
            };
            clone.Normalize();
            return clone;
        }

        private void AddLog(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            ActivityLog.Insert(0, entry);
            while (ActivityLog.Count > 40)
            {
                ActivityLog.RemoveAt(ActivityLog.Count - 1);
            }
        }

        private bool PersistRoutesWithFeedback()
        {
            if (RouteStore.TrySave(SavedRoutes, out var error))
            {
                return true;
            }

            LastStatus = error ?? "Route save failed.";
            AddLog(LastStatus);
            return false;
        }

        private void ShowHelpWindow()
        {
            try
            {
                var window = new Views.WebwalkingInfoWindow();
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                LastStatus = $"Could not open webwalking help: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private static void RefreshCommandStates() => CommandManager.InvalidateRequerySuggested();

        public void OnActivated()
        {
            if (_disposed)
            {
                return;
            }

            if (_isActive)
            {
                UpdateCurrentTile();
                return;
            }

            _isActive = true;
            if (!_refreshTimer.IsEnabled)
            {
                _refreshTimer.Start();
            }

            UpdateCurrentTile();
        }

        public void OnDeactivated()
        {
            if (_disposed || !_isActive)
            {
                return;
            }

            _isActive = false;
            try
            {
                _refreshTimer.Stop();
            }
            catch
            {
                // ignore
            }

            if (IsRunning)
            {
                StopRun();
            }
        }

        private void OnRefreshTick(object? sender, EventArgs e) => UpdateCurrentTile();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            OnDeactivated();
            _disposed = true;

            try
            {
                _refreshTimer.Stop();
            }
            catch
            {
                // ignore
            }

            _refreshTimer.Tick -= OnRefreshTick;
            try
            {
                CurrentRoute.CollectionChanged -= _currentRouteChangedHandler;
            }
            catch
            {
                // ignore
            }

            try
            {
                _runCts?.Cancel();
            }
            catch
            {
                // ignore
            }

            try
            {
                _runCts?.Dispose();
            }
            catch
            {
                // ignore
            }

            _runCts = null;
        }
    }
}
