using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    public sealed class DungeoneeringDebugViewModel : BaseViewModel, IActivatableViewModel, IDisposable
    {
        public sealed class InterfaceProbeRow
        {
            public string Query { get; init; } = string.Empty;
            public bool RootFound { get; init; }
            public string RootId { get; init; } = "-";
            public int StaticMatches { get; init; }
            public string Notes { get; init; } = string.Empty;
        }

        public sealed class DgSignalRow
        {
            public string Kind { get; init; } = string.Empty;
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Action { get; init; } = string.Empty;
            public string Distance { get; init; } = string.Empty;
            public string Tile { get; init; } = string.Empty;
        }

        private readonly DispatcherTimer _timer;
        private readonly EventHandler _timerTickHandler;
        private bool _isActive;
        private bool _disposed;

        private bool _autoRefresh = true;
        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                if (SetProperty(ref _autoRefresh, value))
                {
                    UpdateTimerState();
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
                    UpdateTimerState();
                }
            }
        }

        private bool _enableInterfaceTextScan;
        public bool EnableInterfaceTextScan
        {
            get => _enableInterfaceTextScan;
            set => SetProperty(ref _enableInterfaceTextScan, value);
        }

        private int _scanRadius = 18;
        public int ScanRadius
        {
            get => _scanRadius;
            set => SetProperty(ref _scanRadius, Math.Clamp(value, 6, 40));
        }

        private string _interfaceQueryText =
            "Dungeon entrance\nDungeon exit\nDungeoneering\nParty\nComplexity\nFloor\nRing of kinship";
        public string InterfaceQueryText
        {
            get => _interfaceQueryText;
            set => SetProperty(ref _interfaceQueryText, value);
        }

        private string _status = "Ready. Click Refresh Snapshot.";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _snapshotText = "-";
        public string SnapshotText
        {
            get => _snapshotText;
            set => SetProperty(ref _snapshotText, value);
        }

        private string _lastUpdatedText = "-";
        public string LastUpdatedText
        {
            get => _lastUpdatedText;
            set => SetProperty(ref _lastUpdatedText, value);
        }

        private int _doorLikeCount;
        public int DoorLikeCount
        {
            get => _doorLikeCount;
            set => SetProperty(ref _doorLikeCount, value);
        }

        private int _puzzleLikeCount;
        public int PuzzleLikeCount
        {
            get => _puzzleLikeCount;
            set => SetProperty(ref _puzzleLikeCount, value);
        }

        private int _resourceLikeCount;
        public int ResourceLikeCount
        {
            get => _resourceLikeCount;
            set => SetProperty(ref _resourceLikeCount, value);
        }

        private int _keyGroundCount;
        public int KeyGroundCount
        {
            get => _keyGroundCount;
            set => SetProperty(ref _keyGroundCount, value);
        }

        private int _hostileNpcCount;
        public int HostileNpcCount
        {
            get => _hostileNpcCount;
            set => SetProperty(ref _hostileNpcCount, value);
        }

        private int _partyNearbyCount;
        public int PartyNearbyCount
        {
            get => _partyNearbyCount;
            set => SetProperty(ref _partyNearbyCount, value);
        }

        private int _interfaceHits;
        public int InterfaceHits
        {
            get => _interfaceHits;
            set => SetProperty(ref _interfaceHits, value);
        }

        public ObservableCollection<InterfaceProbeRow> InterfaceProbes { get; } = new();
        public ObservableCollection<DgSignalRow> Signals { get; } = new();

        public ICommand RefreshSnapshotCommand { get; }
        public ICommand CopySnapshotCommand { get; }
        public ICommand CopySignalsCommand { get; }
        public ICommand ClearCommand { get; }

        public DungeoneeringDebugViewModel()
        {
            RefreshSnapshotCommand = new RelayCommand(_ => RefreshSnapshot());
            CopySnapshotCommand = new RelayCommand(_ => CopySnapshot());
            CopySignalsCommand = new RelayCommand(_ => CopySignals());
            ClearCommand = new RelayCommand(_ => ClearData());

            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1.2)
            };
            _timerTickHandler = (_, _) =>
            {
                if (_isActive && AutoRefresh && !FreezeRefresh)
                {
                    RefreshSnapshot();
                }
            };
            _timer.Tick += _timerTickHandler;
        }

        public void OnActivated()
        {
            _isActive = true;
            UpdateTimerState();
            RefreshSnapshot();
        }

        public void OnDeactivated()
        {
            _isActive = false;
            UpdateTimerState();
        }

        private void UpdateTimerState()
        {
            if (_disposed)
            {
                return;
            }

            if (_isActive && AutoRefresh && !FreezeRefresh)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        private void RefreshSnapshot()
        {
            try
            {
                var now = DateTime.Now;
                var lpTile = LocalPlayer.GetTilePosition();
                var lpName = string.IsNullOrWhiteSpace(Game.LocalPlayerName) ? LocalPlayer.Name : Game.LocalPlayerName;
                var hp = LocalPlayer.GetHealthPercent();
                var prayer = LocalPlayer.GetPrayerPercent();
                var inCombat = LocalPlayer.IsInCombat();
                var targeting = LocalPlayer.IsTargeting();
                var interacting = LocalPlayer.GetInteractingWith();

                var radius = Math.Clamp(ScanRadius, 6, 40);
                var roomSignals = DungeoneeringProbes.BuildRoomSignals(radius, maxCount: 240, includeNpcs: true);
                var party = DungeoneeringProbes.BuildPartyCandidates(maxDistance: 35, maxCount: 24, includeFriendChat: true);
                var floorHints = DungeoneeringProbes.BuildFloorHints(maxMessages: 20);
                var roomGraph = DungeoneeringRoomGraph.GetSnapshot();

                var objectRows = roomSignals.Items
                    .OrderBy(s => s.Distance)
                    .Select(s => new DgSignalRow
                    {
                        Kind = s.Kind,
                        Id = s.Id,
                        Name = s.Name,
                        Action = s.Action,
                        Distance = s.Distance.ToString("F1"),
                        Tile = $"[{s.Tile.X},{s.Tile.Y},{s.Tile.Z}]"
                    })
                    .ToList();

                foreach (var p in party.NearbyCandidates.OrderBy(p => p.Distance))
                {
                    objectRows.Add(new DgSignalRow
                    {
                        Kind = "PartyCandidate",
                        Id = p.Id,
                        Name = p.Name,
                        Action = $"Cmb={p.CombatLevel} HP={p.Health}",
                        Distance = p.Distance.ToString("F1"),
                        Tile = $"[{p.Tile.X},{p.Tile.Y},{p.Tile.Z}]"
                    });
                }

                var doorLike = roomSignals.Items.Count(i => i.Kind == DungeoneeringSignalKind.DoorLike.ToString());
                var puzzleLike = roomSignals.Items.Count(i => i.Kind == DungeoneeringSignalKind.PuzzleLike.ToString());
                var resourceLike = roomSignals.Items.Count(i => i.Kind == DungeoneeringSignalKind.ResourceLike.ToString());
                var keyGround = roomSignals.Items.Count(i => i.Kind == DungeoneeringSignalKind.KeyGround.ToString());
                var hostileCount = roomSignals.Items.Count(i => i.Kind.Equals("HostileNpc", StringComparison.OrdinalIgnoreCase));

                var probeRows = BuildInterfaceProbeRows();
                var uiHits = probeRows.Count(r => r.RootFound || r.StaticMatches > 0);

                InterfaceProbes.Clear();
                foreach (var row in probeRows)
                {
                    InterfaceProbes.Add(row);
                }

                Signals.Clear();
                foreach (var row in objectRows.OrderBy(r => r.Kind).ThenBy(r => r.Distance))
                {
                    Signals.Add(row);
                }

                DoorLikeCount = doorLike;
                PuzzleLikeCount = puzzleLike;
                ResourceLikeCount = resourceLike;
                KeyGroundCount = keyGround;
                HostileNpcCount = hostileCount;
                PartyNearbyCount = party.NearbyCount;
                InterfaceHits = uiHits;

                LastUpdatedText = now.ToString("HH:mm:ss");
                SnapshotText = BuildSnapshotText(
                    now,
                    lpName,
                    lpTile,
                    hp,
                    prayer,
                    inCombat,
                    targeting,
                    interacting,
                    radius,
                    roomSignals.Returned,
                    uiHits,
                    floorHints,
                    roomGraph);
                Status = "Snapshot refreshed.";
            }
            catch (Exception ex)
            {
                Status = $"Refresh error: {ex.Message}";
            }
        }

        private List<InterfaceProbeRow> BuildInterfaceProbeRows()
        {
            var rows = new List<InterfaceProbeRow>();
            var queries = (InterfaceQueryText ?? string.Empty)
                .Split(new[] { '\r', '\n', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(q => q.Trim())
                .Where(q => q.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var query in queries)
            {
                try
                {
                    var root = Interfaces.GetByString(query);
                    var staticHits = Interfaces.GetByStringStatic(query);
                    var rootFound = root.Id1 > 0;
                    rows.Add(new InterfaceProbeRow
                    {
                        Query = query,
                        RootFound = rootFound,
                        RootId = rootFound ? $"{root.Id1}:{root.Id2}:{root.Id3}" : "-",
                        StaticMatches = staticHits.Count,
                        Notes = staticHits.Count > 0 ? "static match(es) present" : "no static match"
                    });
                }
                catch (Exception ex)
                {
                    rows.Add(new InterfaceProbeRow
                    {
                        Query = query,
                        RootFound = false,
                        RootId = "-",
                        StaticMatches = 0,
                        Notes = $"error: {ex.Message}"
                    });
                }
            }

            if (EnableInterfaceTextScan)
            {
                try
                {
                    var scan = Interfaces.Scan(rootId: -1, getOnlyTarget: false, textOnly: false, includeHidden: false);
                    var dungTerms = new[] { "dungeon", "dungeoneering", "party", "floor", "complexity", "kinship" };
                    var textMatches = scan.Count(c =>
                    {
                        var text = $"{c.TextIds} {c.TextItem} {c.FullPath}".ToLowerInvariant();
                        return dungTerms.Any(t => text.Contains(t));
                    });

                    rows.Add(new InterfaceProbeRow
                    {
                        Query = "(deep text scan)",
                        RootFound = textMatches > 0,
                        RootId = "-",
                        StaticMatches = textMatches,
                        Notes = "count of visible interface components containing dungeoneering terms"
                    });
                }
                catch (Exception ex)
                {
                    rows.Add(new InterfaceProbeRow
                    {
                        Query = "(deep text scan)",
                        RootFound = false,
                        RootId = "-",
                        StaticMatches = 0,
                        Notes = $"error: {ex.Message}"
                    });
                }
            }

            return rows;
        }

        private static string BuildSnapshotText(
            DateTime now,
            string lpName,
            (int x, int y, int z) lpTile,
            int hp,
            int prayer,
            bool inCombat,
            bool targeting,
            string interacting,
            int radius,
            int nearbyObjects,
            int interfaceHits,
            DgFloorHintsResult floorHints,
            DgRoomGraphSnapshot roomGraph)
        {
            var sb = new StringBuilder(768);
            var currentRoom = roomGraph.CurrentRoomX.HasValue && roomGraph.CurrentRoomY.HasValue
                ? $"[{roomGraph.CurrentRoomX},{roomGraph.CurrentRoomY}]"
                : "-";

            sb.AppendLine($"Time: {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Player: {lpName}");
            sb.AppendLine($"Tile: [{lpTile.x},{lpTile.y},{lpTile.z}]");
            sb.AppendLine($"Combat: inCombat={inCombat} targeting={targeting} interacting='{interacting}'");
            sb.AppendLine($"Vitals: HP%={hp} Prayer%={prayer}");
            sb.AppendLine($"Probe: radius={radius} nearbyObjects={nearbyObjects} interfaceHits={interfaceHits}");
            sb.AppendLine($"Floor hints: floor={floorHints.InferredFloor?.ToString() ?? "?"} complexity={floorHints.InferredComplexity?.ToString() ?? "?"} maxFloorByLevel={floorHints.MaxFloorByLevel}");
            sb.AppendLine($"Ring: inv={floorHints.RingOfKinshipInInventory} equipped={floorHints.RingOfKinshipEquipped} available={floorHints.RingOfKinshipAvailable}");
            sb.AppendLine($"Graph: source={roomGraph.Source} state={roomGraph.State} current={currentRoom} rooms={roomGraph.Rooms.Count} edges={roomGraph.Edges.Count} lockedDoors={roomGraph.LockedDoors.Count}");
            sb.AppendLine("Note: This is debug-only probing. Do not depend on this tooling in production scripts.");
            return sb.ToString();
        }

        private void CopySnapshot()
        {
            if (string.IsNullOrWhiteSpace(SnapshotText))
            {
                return;
            }

            Clipboard.SetText(SnapshotText);
            Status = "Snapshot copied to clipboard.";
        }

        private void CopySignals()
        {
            if (Signals.Count == 0)
            {
                return;
            }

            var text = string.Join(Environment.NewLine, Signals.Select(s =>
                $"{s.Kind} | {s.Name}#{s.Id} | dist={s.Distance} | tile={s.Tile} | action={s.Action}"));
            Clipboard.SetText(text);
            Status = "Signals copied to clipboard.";
        }

        private void ClearData()
        {
            InterfaceProbes.Clear();
            Signals.Clear();
            SnapshotText = "-";
            DoorLikeCount = 0;
            PuzzleLikeCount = 0;
            ResourceLikeCount = 0;
            KeyGroundCount = 0;
            HostileNpcCount = 0;
            PartyNearbyCount = 0;
            InterfaceHits = 0;
            LastUpdatedText = "-";
            Status = "Cleared.";
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Stop();
            _timer.Tick -= _timerTickHandler;
        }
    }
}
