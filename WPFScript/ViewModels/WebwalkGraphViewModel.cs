using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    public sealed class GraphNodeDisplay
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int X { get; init; }
        public int Y { get; init; }
        public int Z { get; init; }
        public string Tags { get; init; } = string.Empty;
        public bool Enabled { get; init; } = true;
    }

    public sealed class GraphRouteDisplay
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public int WaypointCount { get; init; }
        public override string ToString() => string.IsNullOrWhiteSpace(Id) ? Name : Id;
    }

    public sealed class GraphEdgeDisplay
    {
        public string Id { get; init; } = string.Empty;
        public string From { get; init; } = string.Empty;
        public string To { get; init; } = string.Empty;
        public string Kind { get; init; } = string.Empty;
        public string? RouteId { get; init; }
        public int CostMs { get; init; }
        public int DangerLevel { get; init; }
        public bool Enabled { get; init; } = true;
        public bool HasBrokenRef { get; init; }
        public string IssueSummary { get; init; } = string.Empty;
    }

    public sealed class WebwalkGraphViewModel : BaseViewModel, IDisposable, IActivatableViewModel
    {
        private readonly DispatcherTimer _refreshTimer;
        private readonly DispatcherTimer _recorderTimer;
        private CancellationTokenSource? _pathRunCts;
        private bool _isActive;
        private bool _disposed;

        // ── Status ────────────────────────────────────────────────────────────────
        private string _lastStatus = "Ready.";
        private string _currentTile = "--";
        public string LastStatus { get => _lastStatus; set => SetProperty(ref _lastStatus, value); }
        public string CurrentTile { get => _currentTile; set => SetProperty(ref _currentTile, value); }

        // ── Recorder ─────────────────────────────────────────────────────────────
        private bool _isRecording;
        private int _recordIntervalSeconds = 3;
        private int _minWaypointDistance = 4;
        private string _recordRouteName = "recorded.route";
        private WorldPoint? _lastRecordedPoint;
        private string _recordingSummary = "0 waypoints.";

        public bool IsRecording { get => _isRecording; set { SetProperty(ref _isRecording, value); RefreshCommandStates(); } }
        public int RecordIntervalSeconds { get => _recordIntervalSeconds; set => SetProperty(ref _recordIntervalSeconds, value); }
        public int MinWaypointDistance { get => _minWaypointDistance; set => SetProperty(ref _minWaypointDistance, value); }
        public string RecordRouteName { get => _recordRouteName; set { SetProperty(ref _recordRouteName, value); RefreshCommandStates(); } }
        public string RecordingSummary { get => _recordingSummary; set => SetProperty(ref _recordingSummary, value); }

        public ObservableCollection<string> RecordedTiles { get; } = new();

        private readonly List<WebwalkRecordedSample> _recordedSamples = new();

        // ── Graph view ────────────────────────────────────────────────────────────
        public ObservableCollection<GraphNodeDisplay> Nodes { get; } = new();
        public ObservableCollection<GraphEdgeDisplay> Edges { get; } = new();
        public ObservableCollection<GraphRouteDisplay> Routes { get; } = new();
        public ObservableCollection<string> ValidationIssues { get; } = new();
        public ObservableCollection<string> ActivityLog { get; } = new();

        private GraphNodeDisplay? _selectedNode;
        private GraphEdgeDisplay? _selectedEdge;
        private GraphRouteDisplay? _selectedRoute;
        private bool _hasValidationErrors;
        private bool _hasValidationWarnings;
        private bool _isPathRunning;
        private string _pathFromNodeId = string.Empty;
        private string _pathToNodeId = string.Empty;
        private string _pathPreview = "Select endpoints and preview a path.";
        private string _pathRunStatus = "Idle.";

        public GraphNodeDisplay? SelectedNode { get => _selectedNode; set { if (SetProperty(ref _selectedNode, value)) RefreshCommandStates(); } }
        public GraphEdgeDisplay? SelectedEdge { get => _selectedEdge; set { if (SetProperty(ref _selectedEdge, value)) RefreshCommandStates(); } }
        public GraphRouteDisplay? SelectedRoute { get => _selectedRoute; set { if (SetProperty(ref _selectedRoute, value)) { if (value != null) NewEdgeRouteId = value.Id; RefreshCommandStates(); } } }
        public bool HasValidationErrors { get => _hasValidationErrors; set => SetProperty(ref _hasValidationErrors, value); }
        public bool HasValidationWarnings { get => _hasValidationWarnings; set => SetProperty(ref _hasValidationWarnings, value); }
        public bool IsPathRunning { get => _isPathRunning; set { SetProperty(ref _isPathRunning, value); RefreshCommandStates(); } }
        public string PathFromNodeId { get => _pathFromNodeId; set { SetProperty(ref _pathFromNodeId, value); RefreshCommandStates(); } }
        public string PathToNodeId { get => _pathToNodeId; set { SetProperty(ref _pathToNodeId, value); RefreshCommandStates(); } }
        public string PathPreview { get => _pathPreview; set => SetProperty(ref _pathPreview, value); }
        public string PathRunStatus { get => _pathRunStatus; set => SetProperty(ref _pathRunStatus, value); }

        // ── Node editor ───────────────────────────────────────────────────────────
        private string _newNodeId = string.Empty;
        private string _newNodeName = string.Empty;
        private int _newNodeX;
        private int _newNodeY;
        private int _newNodeZ;
        private string _newNodeTags = string.Empty;
        public string NewNodeId { get => _newNodeId; set { SetProperty(ref _newNodeId, value); RefreshCommandStates(); } }
        public string NewNodeName { get => _newNodeName; set { SetProperty(ref _newNodeName, value); RefreshCommandStates(); } }
        public int NewNodeX { get => _newNodeX; set => SetProperty(ref _newNodeX, value); }
        public int NewNodeY { get => _newNodeY; set => SetProperty(ref _newNodeY, value); }
        public int NewNodeZ { get => _newNodeZ; set => SetProperty(ref _newNodeZ, value); }
        public string NewNodeTags { get => _newNodeTags; set => SetProperty(ref _newNodeTags, value); }

        // ── Edge editor ───────────────────────────────────────────────────────────
        private string _newEdgeId = string.Empty;
        private string _newEdgeFrom = string.Empty;
        private string _newEdgeTo = string.Empty;
        private string _newEdgeKind = "route";
        private string _newEdgeRouteId = string.Empty;
        private int _newEdgeCostMs = 15000;
        private int _newEdgeDangerLevel;
        public string NewEdgeId { get => _newEdgeId; set { SetProperty(ref _newEdgeId, value); RefreshCommandStates(); } }
        public string NewEdgeFrom { get => _newEdgeFrom; set { SetProperty(ref _newEdgeFrom, value); RefreshCommandStates(); } }
        public string NewEdgeTo { get => _newEdgeTo; set { SetProperty(ref _newEdgeTo, value); RefreshCommandStates(); } }
        public string NewEdgeKind { get => _newEdgeKind; set { SetProperty(ref _newEdgeKind, value); RefreshCommandStates(); } }
        public string NewEdgeRouteId { get => _newEdgeRouteId; set { SetProperty(ref _newEdgeRouteId, value); RefreshCommandStates(); } }
        public int NewEdgeCostMs { get => _newEdgeCostMs; set => SetProperty(ref _newEdgeCostMs, value); }
        public int NewEdgeDangerLevel { get => _newEdgeDangerLevel; set => SetProperty(ref _newEdgeDangerLevel, value); }

        // ── Commands ──────────────────────────────────────────────────────────────
        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand ClearRecordingCommand { get; }
        public ICommand SaveRecordingAsRouteCommand { get; }
        public ICommand UseCurrentTileForNodeCommand { get; }
        public ICommand SaveNodeCommand { get; }
        public ICommand LoadSelectedNodeCommand { get; }
        public ICommand DeleteSelectedNodeCommand { get; }
        public ICommand UseSelectedNodeAsFromCommand { get; }
        public ICommand UseSelectedNodeAsToCommand { get; }
        public ICommand UseSelectedRouteForEdgeCommand { get; }
        public ICommand ValidateSelectedRouteCommand { get; }
        public ICommand LoadSelectedEdgeCommand { get; }
        public ICommand DeleteSelectedEdgeCommand { get; }
        public ICommand SaveEdgeCommand { get; }
        public ICommand SaveRecordingAndEdgeCommand { get; }
        public ICommand PreviewPathCommand { get; }
        public ICommand RunPreviewPathCommand { get; }
        public ICommand StopPathCommand { get; }
        public ICommand RefreshGraphCommand { get; }
        public ICommand ValidateGraphCommand { get; }
        public ICommand OpenGraphFileCommand { get; }

        public WebwalkGraphViewModel()
        {
            StartRecordingCommand = new RelayCommand(_ => StartRecording(), _ => !IsRecording);
            StopRecordingCommand = new RelayCommand(_ => StopRecording(), _ => IsRecording);
            ClearRecordingCommand = new RelayCommand(_ => ClearRecording(), _ => !IsRecording);
            SaveRecordingAsRouteCommand = new RelayCommand(_ => SaveRecordingAsRoute(), _ => !IsRecording && _recordedSamples.Count > 0 && !string.IsNullOrWhiteSpace(RecordRouteName));
            UseCurrentTileForNodeCommand = new RelayCommand(_ => UseCurrentTileForNode(), _ => !IsRecording);
            SaveNodeCommand = new RelayCommand(_ => SaveNode(), _ => !string.IsNullOrWhiteSpace(NewNodeId) && !string.IsNullOrWhiteSpace(NewNodeName));
            LoadSelectedNodeCommand = new RelayCommand(_ => LoadSelectedNode(), _ => SelectedNode != null);
            DeleteSelectedNodeCommand = new RelayCommand(_ => DeleteSelectedNode(), _ => SelectedNode != null);
            UseSelectedNodeAsFromCommand = new RelayCommand(_ => UseSelectedNodeAsFrom(), _ => SelectedNode != null);
            UseSelectedNodeAsToCommand = new RelayCommand(_ => UseSelectedNodeAsTo(), _ => SelectedNode != null);
            UseSelectedRouteForEdgeCommand = new RelayCommand(_ => UseSelectedRouteForEdge(), _ => SelectedRoute != null);
            ValidateSelectedRouteCommand = new RelayCommand(_ => ValidateSelectedRoute(), _ => SelectedRoute != null);
            LoadSelectedEdgeCommand = new RelayCommand(_ => LoadSelectedEdge(), _ => SelectedEdge != null);
            DeleteSelectedEdgeCommand = new RelayCommand(_ => DeleteSelectedEdge(), _ => SelectedEdge != null);
            SaveEdgeCommand = new RelayCommand(_ => SaveEdge(), _ => CanSaveEdge(requireEdgeId: true));
            SaveRecordingAndEdgeCommand = new RelayCommand(_ => SaveRecordingAndEdge(), _ => CanSaveRecordingAndEdge());
            PreviewPathCommand = new RelayCommand(_ => PreviewPath(), _ => CanPlanPath());
            RunPreviewPathCommand = new RelayCommand(_ => RunPreviewPath(), _ => CanPlanPath() && !IsPathRunning);
            StopPathCommand = new RelayCommand(_ => StopPath(), _ => IsPathRunning);
            RefreshGraphCommand = new RelayCommand(_ => RefreshGraph());
            ValidateGraphCommand = new RelayCommand(_ => ValidateGraph());
            OpenGraphFileCommand = new RelayCommand(_ => OpenGraphFile());

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(750) };
            _refreshTimer.Tick += (_, __) => UpdateCurrentTile();

            _recorderTimer = new DispatcherTimer(DispatcherPriority.Background);
            _recorderTimer.Tick += (_, __) => RecordTick();

            RefreshGraph();
            ValidateGraph();
            AddLog("Graph editor ready.");
        }

        public void OnActivated()
        {
            if (_disposed || _isActive) return;
            _isActive = true;
            _refreshTimer.Start();
            UpdateCurrentTile();
        }

        public void OnDeactivated()
        {
            if (_disposed || !_isActive) return;
            _isActive = false;
            _refreshTimer.Stop();
            if (IsRecording) StopRecording();
        }

        private void StartRecording()
        {
            _lastRecordedPoint = null;
            IsRecording = true;
            _recorderTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(Webwalking.GameTickMs, RecordIntervalSeconds * 1000));
            _recorderTimer.Start();
            AddLog($"Recording started (interval={Math.Max(1, RecordIntervalSeconds)}s, minDist={MinWaypointDistance} tiles).");
        }

        private void StopRecording()
        {
            _recorderTimer.Stop();
            IsRecording = false;
            AddLog($"Recording stopped. {_recordedSamples.Count} waypoints captured.");
        }

        private void ClearRecording()
        {
            _recordedSamples.Clear();
            RecordedTiles.Clear();
            _lastRecordedPoint = null;
            UpdateRecordingSummary();
            AddLog("Recording cleared.");
        }

        private void RecordTick()
        {
            try
            {
                var tile = LocalPlayer.GetTilePosition();
                var pos = new WorldPoint(tile.x, tile.y, tile.z);

                if (!WebwalkAuthoring.ShouldRecordSample(pos, _lastRecordedPoint, MinWaypointDistance))
                    return;

                _recordedSamples.Add(new WebwalkRecordedSample { Position = pos });
                _lastRecordedPoint = pos;
                RecordedTiles.Insert(0, $"[{_recordedSamples.Count}] ({tile.x}, {tile.y}, {tile.z})");
                if (RecordedTiles.Count > 100) RecordedTiles.RemoveAt(RecordedTiles.Count - 1);
                UpdateRecordingSummary();
            }
            catch { }
        }

        private void SaveRecordingAsRoute()
        {
            TrySaveRecordingRoute();
        }

        private bool TrySaveRecordingRoute()
        {
            if (_recordedSamples.Count == 0)
            {
                LastStatus = "No waypoints recorded.";
                return false;
            }

            var name = RecordRouteName.Trim();
            var route = WebwalkAuthoring.CreateRouteFromSamples(
                name,
                _recordedSamples,
                new WebwalkRecordingOptions { MinWaypointDistance = MinWaypointDistance });
            var summary = WebwalkAuthoring.SummarizeSamples(_recordedSamples);

            if (Webwalking.TrySaveRoute(route, out var error))
            {
                LastStatus = $"Saved route '{name}' ({summary.WaypointCount} waypoints, ~{summary.EstimatedRunTicks} ticks).";
                AddLog(LastStatus);
                NewEdgeRouteId = route.Id ?? name.ToLowerInvariant().Replace(' ', '_');
                RefreshRoutes();
                SelectedRoute = Routes.FirstOrDefault(r => string.Equals(r.Id, NewEdgeRouteId, StringComparison.OrdinalIgnoreCase));
                AutoFillEdgeId();
                return true;
            }
            else
            {
                LastStatus = $"Save failed: {error}";
                AddLog(LastStatus);
                return false;
            }
        }

        private void SaveRecordingAndEdge()
        {
            if (!TrySaveRecordingRoute())
                return;

            if (string.IsNullOrWhiteSpace(NewEdgeRouteId))
                return;

            if (string.IsNullOrWhiteSpace(NewEdgeId))
                NewEdgeId = GenerateEdgeId(NewEdgeFrom, NewEdgeTo, NewEdgeRouteId);

            SaveEdge();
        }

        private void UpdateRecordingSummary()
        {
            var summary = WebwalkAuthoring.SummarizeSamples(_recordedSamples);
            RecordingSummary = summary.WaypointCount == 0
                ? "0 waypoints."
                : $"{summary.WaypointCount} waypoints, ~{summary.ApproxDistanceTiles} tiles, ~{summary.EstimatedRunTicks} ticks / {summary.EstimatedRunMs / 1000.0:0.0}s running.";
            RefreshCommandStates();
        }

        private void UseCurrentTileForNode()
        {
            try
            {
                var tile = LocalPlayer.GetTilePosition();
                NewNodeX = tile.x;
                NewNodeY = tile.y;
                NewNodeZ = tile.z;
                LastStatus = $"Copied current tile ({tile.x}, {tile.y}, {tile.z}) to node editor.";
            }
            catch (Exception ex)
            {
                LastStatus = $"Failed to read tile: {ex.Message}";
            }
        }

        private void SaveNode()
        {
            var node = new WebwalkGraphNode
            {
                Id = NewNodeId.Trim(),
                Name = NewNodeName.Trim(),
                X = NewNodeX, Y = NewNodeY, Z = NewNodeZ,
                Tags = NewNodeTags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                Enabled = true
            };

            if (WebwalkGraph.TrySaveNode(node, out var error))
            {
                LastStatus = $"Saved node '{node.Id}'.";
                AddLog(LastStatus);
                RefreshGraph();
                PathFromNodeId = string.IsNullOrWhiteSpace(PathFromNodeId) ? node.Id : PathFromNodeId;
            }
            else
            {
                LastStatus = $"Node save failed: {error}";
                AddLog(LastStatus);
            }
        }

        private void LoadSelectedNode()
        {
            if (SelectedNode == null) return;
            NewNodeId = SelectedNode.Id;
            NewNodeName = SelectedNode.Name;
            NewNodeX = SelectedNode.X;
            NewNodeY = SelectedNode.Y;
            NewNodeZ = SelectedNode.Z;
            NewNodeTags = SelectedNode.Tags;
            LastStatus = $"Loaded node '{SelectedNode.Id}' into the node editor.";
        }

        private void DeleteSelectedNode()
        {
            if (SelectedNode == null) return;
            var nodeId = SelectedNode.Id;
            if (WebwalkGraph.TryDeleteNode(nodeId, out var error))
            {
                LastStatus = $"Deleted node '{nodeId}' and any referenced edges.";
                AddLog(LastStatus);
                if (string.Equals(PathFromNodeId, nodeId, StringComparison.OrdinalIgnoreCase)) PathFromNodeId = string.Empty;
                if (string.Equals(PathToNodeId, nodeId, StringComparison.OrdinalIgnoreCase)) PathToNodeId = string.Empty;
                RefreshGraph();
                ValidateGraph();
            }
            else
            {
                LastStatus = $"Node delete failed: {error}";
                AddLog(LastStatus);
            }
        }

        private void UseSelectedNodeAsFrom()
        {
            if (SelectedNode == null) return;
            NewEdgeFrom = SelectedNode.Id;
            PathFromNodeId = SelectedNode.Id;
            AutoFillEdgeId();
        }

        private void UseSelectedNodeAsTo()
        {
            if (SelectedNode == null) return;
            NewEdgeTo = SelectedNode.Id;
            PathToNodeId = SelectedNode.Id;
            AutoFillEdgeId();
        }

        private void UseSelectedRouteForEdge()
        {
            if (SelectedRoute == null) return;
            NewEdgeRouteId = SelectedRoute.Id;
            if (NewEdgeCostMs <= 1000)
                NewEdgeCostMs = EstimateRouteCostMs(SelectedRoute.WaypointCount);
            AutoFillEdgeId();
        }

        private void ValidateSelectedRoute()
        {
            if (SelectedRoute == null) return;
            if (!Webwalking.TryGetRoute(SelectedRoute.Id, out var route))
            {
                LastStatus = $"Route '{SelectedRoute.Id}' was not found.";
                AddLog(LastStatus);
                return;
            }

            var result = Webwalking.ValidateRoute(ToStoredRoute(route));
            LastStatus = result.IsValid
                ? $"Route '{route.Id}' is valid ({result.Warnings.Count} warning(s))."
                : $"Route '{route.Id}' has {result.Errors.Count} error(s), {result.Warnings.Count} warning(s).";
            AddLog(LastStatus);

            foreach (var issue in result.Issues.Take(8))
                AddLog($"Route {issue.Severity}: {issue.Code} - {issue.Message}");
        }

        private void LoadSelectedEdge()
        {
            if (SelectedEdge == null) return;
            NewEdgeId = SelectedEdge.Id;
            NewEdgeFrom = SelectedEdge.From;
            NewEdgeTo = SelectedEdge.To;
            NewEdgeKind = SelectedEdge.Kind;
            NewEdgeRouteId = SelectedEdge.RouteId ?? string.Empty;
            NewEdgeCostMs = SelectedEdge.CostMs;
            NewEdgeDangerLevel = SelectedEdge.DangerLevel;
            PathFromNodeId = SelectedEdge.From;
            PathToNodeId = SelectedEdge.To;
            LastStatus = $"Loaded edge '{SelectedEdge.Id}' into the edge editor.";
        }

        private void DeleteSelectedEdge()
        {
            if (SelectedEdge == null) return;
            var edgeId = SelectedEdge.Id;
            if (WebwalkGraph.TryDeleteEdge(edgeId, out var error))
            {
                LastStatus = $"Deleted edge '{edgeId}'.";
                AddLog(LastStatus);
                RefreshGraph();
                ValidateGraph();
            }
            else
            {
                LastStatus = $"Edge delete failed: {error}";
                AddLog(LastStatus);
            }
        }

        private void SaveEdge()
        {
            var kind = string.IsNullOrWhiteSpace(NewEdgeKind) ? "route" : NewEdgeKind.Trim();
            if (!string.Equals(kind, "route", StringComparison.OrdinalIgnoreCase))
            {
                LastStatus = "The WPF graph editor currently saves route edges only. Use MCP for teleport edges.";
                AddLog(LastStatus);
                return;
            }

            var edge = new WebwalkGraphEdge
            {
                Id = NewEdgeId.Trim(),
                FromNodeId = NewEdgeFrom.Trim(),
                ToNodeId = NewEdgeTo.Trim(),
                Kind = kind,
                RouteId = string.IsNullOrWhiteSpace(NewEdgeRouteId) ? null : NewEdgeRouteId.Trim(),
                CostMs = Math.Max(1000, NewEdgeCostMs),
                DangerLevel = NewEdgeDangerLevel,
                Enabled = true
            };

            if (WebwalkGraph.TrySaveEdge(edge, out var error))
            {
                LastStatus = $"Saved edge '{edge.Id}'.";
                AddLog(LastStatus);
                RefreshGraph();
                ValidateGraph();
            }
            else
            {
                LastStatus = $"Edge save failed: {error}";
                AddLog(LastStatus);
            }
        }

        private bool CanSaveEdge(bool requireEdgeId)
        {
            if (requireEdgeId && string.IsNullOrWhiteSpace(NewEdgeId)) return false;
            if (string.IsNullOrWhiteSpace(NewEdgeFrom)) return false;
            if (string.IsNullOrWhiteSpace(NewEdgeTo)) return false;
            if (!string.Equals(NewEdgeKind, "route", StringComparison.OrdinalIgnoreCase)) return false;
            return !string.IsNullOrWhiteSpace(NewEdgeRouteId);
        }

        private bool CanSaveRecordingAndEdge()
        {
            if (IsRecording) return false;
            if (_recordedSamples.Count == 0) return false;
            if (string.IsNullOrWhiteSpace(RecordRouteName)) return false;
            if (string.IsNullOrWhiteSpace(NewEdgeFrom)) return false;
            if (string.IsNullOrWhiteSpace(NewEdgeTo)) return false;
            return string.Equals(NewEdgeKind, "route", StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshGraph()
        {
            WebwalkGraph.ReloadGraph();
            RefreshRoutes();
            var graph = WebwalkGraph.GetGraph();

            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id), StringComparer.OrdinalIgnoreCase);
            var routeIds = new HashSet<string>(Webwalking.GetRoutes().Select(r => r.Id), StringComparer.OrdinalIgnoreCase);

            var selectedNodeId = SelectedNode?.Id;
            var selectedEdgeId = SelectedEdge?.Id;
            Nodes.Clear();
            foreach (var n in graph.Nodes)
                Nodes.Add(new GraphNodeDisplay { Id = n.Id, Name = n.Name, X = n.X, Y = n.Y, Z = n.Z, Tags = string.Join(", ", n.Tags ?? new()), Enabled = n.Enabled });
            SelectedNode = string.IsNullOrWhiteSpace(selectedNodeId) ? null : Nodes.FirstOrDefault(n => string.Equals(n.Id, selectedNodeId, StringComparison.OrdinalIgnoreCase));

            Edges.Clear();
            foreach (var e in graph.Edges)
            {
                var brokenFrom = !e.IsWildcard && !nodeIds.Contains(e.FromNodeId);
                var brokenTo = !nodeIds.Contains(e.ToNodeId);
                var brokenRoute = string.Equals(e.Kind, "route", StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(e.RouteId) || !routeIds.Contains(e.RouteId));
                var unsupported = !string.Equals(e.Kind, "route", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(e.Kind, "lodestone", StringComparison.OrdinalIgnoreCase);
                var issues = new List<string>();
                if (brokenFrom) issues.Add("from node");
                if (brokenTo) issues.Add("to node");
                if (brokenRoute) issues.Add("route");
                if (unsupported) issues.Add("kind");
                Edges.Add(new GraphEdgeDisplay
                {
                    Id = e.Id, From = e.FromNodeId, To = e.ToNodeId, Kind = e.Kind,
                    RouteId = e.RouteId, CostMs = e.CostMs, DangerLevel = e.DangerLevel,
                    Enabled = e.Enabled, HasBrokenRef = issues.Count > 0,
                    IssueSummary = issues.Count == 0 ? string.Empty : "Broken: " + string.Join(", ", issues)
                });
            }
            SelectedEdge = string.IsNullOrWhiteSpace(selectedEdgeId) ? null : Edges.FirstOrDefault(e => string.Equals(e.Id, selectedEdgeId, StringComparison.OrdinalIgnoreCase));

            LastStatus = $"Graph: {Nodes.Count} nodes, {Edges.Count} edges. Store: {WebwalkGraph.GetGraphStorePath()}";
        }

        private void RefreshRoutes()
        {
            var selectedRouteId = SelectedRoute?.Id;
            Webwalking.ReloadRoutes();
            Routes.Clear();
            foreach (var route in Webwalking.GetRoutes().OrderBy(r => r.Category).ThenBy(r => r.Name))
            {
                Routes.Add(new GraphRouteDisplay
                {
                    Id = route.Id,
                    Name = route.Name,
                    Category = route.Category,
                    WaypointCount = route.Waypoints.Count
                });
            }
            SelectedRoute = string.IsNullOrWhiteSpace(selectedRouteId) ? null : Routes.FirstOrDefault(r => string.Equals(r.Id, selectedRouteId, StringComparison.OrdinalIgnoreCase));
        }

        private void PreviewPath()
        {
            if (!TryBuildPath(out var path, out var message))
            {
                PathPreview = message;
                LastStatus = PathPreview;
                AddLog(PathPreview);
                return;
            }

            var edgeList = path.Edges.Count == 0
                ? "(already at target)"
                : string.Join(" -> ", path.Edges.Select(e => e.Id));
            PathPreview = $"{path.Edges.Count} edge(s), ~{path.TotalCostMs / 1000.0:0.0}s: {edgeList}";
            LastStatus = PathPreview;
            AddLog("Path preview: " + PathPreview);
        }

        private async void RunPreviewPath()
        {
            if (!TryBuildPath(out var path, out var message))
            {
                PathRunStatus = message;
                LastStatus = message;
                AddLog(message);
                return;
            }

            _pathRunCts?.Dispose();
            _pathRunCts = new CancellationTokenSource();
            IsPathRunning = true;
            PathRunStatus = $"Running {path.Edges.Count} edge(s).";
            LastStatus = PathRunStatus;
            AddLog(PathRunStatus);

            try
            {
                var result = await Webwalking.RunPlanDetailedAsync(path, _pathRunCts.Token).ConfigureAwait(true);
                var edgeSummary = result.Edges.Count == 0
                    ? "no edges"
                    : string.Join(", ", result.Edges.Select((e, i) => $"{i + 1}:{e.EdgeId}={e.Succeeded}"));
                PathRunStatus = $"{result.Status}: {result.Message} ({edgeSummary})";
                LastStatus = PathRunStatus;
                AddLog("Path run: " + PathRunStatus);
            }
            catch (OperationCanceledException)
            {
                PathRunStatus = "Path run cancelled.";
                LastStatus = PathRunStatus;
                AddLog(PathRunStatus);
            }
            catch (Exception ex)
            {
                PathRunStatus = $"Path run error: {ex.Message}";
                LastStatus = PathRunStatus;
                AddLog(PathRunStatus);
            }
            finally
            {
                IsPathRunning = false;
                _pathRunCts?.Dispose();
                _pathRunCts = null;
            }
        }

        private void StopPath()
        {
            _pathRunCts?.Cancel();
            LastStatus = "Stopping path run...";
            AddLog(LastStatus);
        }

        private bool TryBuildPath(out WebwalkGraphPath path, out string message)
        {
            var from = PathFromNodeId.Trim();
            var to = PathToNodeId.Trim();
            var found = WebwalkGraph.FindPath(from, to);
            if (found == null)
            {
                path = new WebwalkGraphPath();
                message = $"No path from '{from}' to '{to}'.";
                return false;
            }

            path = found;
            message = string.Empty;
            return true;
        }

        private bool CanPlanPath()
            => !string.IsNullOrWhiteSpace(PathFromNodeId) && !string.IsNullOrWhiteSpace(PathToNodeId);

        private void ValidateGraph()
        {
            var result = WebwalkGraph.ValidateGraph();
            ValidationIssues.Clear();
            foreach (var issue in result.Issues)
                ValidationIssues.Add($"[{issue.Severity.ToUpper()}] {issue.Code}: {issue.Message}");

            HasValidationErrors = result.Errors.Count > 0;
            HasValidationWarnings = result.Warnings.Count > 0;

            if (result.IsValid && result.Warnings.Count == 0)
                ValidationIssues.Add("Graph is valid — no issues found.");

            AddLog($"Validation: {result.Errors.Count} errors, {result.Warnings.Count} warnings.");
        }

        private void OpenGraphFile()
        {
            try
            {
                var path = WebwalkGraph.GetGraphStorePath();
                if (!System.IO.File.Exists(path))
                    System.IO.File.WriteAllText(path, "{}");
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            catch (Exception ex)
            {
                LastStatus = $"Could not open graph file: {ex.Message}";
            }
        }

        private void UpdateCurrentTile()
        {
            try
            {
                var tile = LocalPlayer.GetTilePosition();
                CurrentTile = $"{tile.x}, {tile.y}, {tile.z}";
            }
            catch { CurrentTile = "--"; }
        }

        private void AddLog(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            ActivityLog.Insert(0, entry);
            while (ActivityLog.Count > 40) ActivityLog.RemoveAt(ActivityLog.Count - 1);
        }

        private void AutoFillEdgeId()
        {
            if (!string.IsNullOrWhiteSpace(NewEdgeId)) return;
            if (string.IsNullOrWhiteSpace(NewEdgeFrom) || string.IsNullOrWhiteSpace(NewEdgeTo)) return;
            NewEdgeId = GenerateEdgeId(NewEdgeFrom, NewEdgeTo, NewEdgeRouteId);
        }

        private static string GenerateEdgeId(string fromNodeId, string toNodeId, string routeId)
        {
            var from = NormalizeIdPart(fromNodeId);
            var to = NormalizeIdPart(toNodeId);
            var route = NormalizeIdPart(routeId);
            return string.IsNullOrWhiteSpace(route)
                ? $"edge.{from}.to.{to}"
                : $"edge.{from}.to.{to}.{route}";
        }

        private static string NormalizeIdPart(string value)
        {
            var chars = (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '.')
                .ToArray();
            var text = new string(chars);
            while (text.Contains("..", StringComparison.Ordinal))
                text = text.Replace("..", ".");
            return text.Trim('.').Length == 0 ? "unnamed" : text.Trim('.');
        }

        private static int EstimateRouteCostMs(int waypointCount)
            => Math.Max(Webwalking.GameTickMs, Math.Max(1, waypointCount) * Webwalking.GameTickMs * 3);

        private static WebwalkingStoredRoute ToStoredRoute(WebwalkingRoute route)
            => new()
            {
                Id = route.Id,
                Name = route.Name,
                Description = route.Description,
                Category = route.Category,
                IsEnabled = route.IsEnabled,
                Tags = route.Tags.ToList(),
                Waypoints = route.Waypoints.Select(wp => new WebwalkingStoredWaypoint
                {
                    Label = wp.Label,
                    X = wp.Point.X,
                    Y = wp.Point.Y,
                    Z = wp.Point.Z,
                    AreaRadius = wp.AreaRadius,
                    ArrivalDistance = wp.ArrivalDistance,
                    TimeoutMs = wp.TimeoutMs,
                    JitterTiles = wp.JitterTiles,
                    ChainWhileMoving = wp.ChainWhileMoving,
                    IsTransition = wp.IsTransition,
                    TransitionObjectIds = wp.TransitionObjectIds.ToList()
                }).ToList()
            };

        private static void RefreshCommandStates() => CommandManager.InvalidateRequerySuggested();

        public void Dispose()
        {
            if (_disposed) return;
            OnDeactivated();
            _disposed = true;
            _refreshTimer.Stop();
            _recorderTimer.Stop();
            _pathRunCts?.Cancel();
            _pathRunCts?.Dispose();
        }
    }
}
