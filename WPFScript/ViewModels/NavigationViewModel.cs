using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    public class NavigationViewModel : BaseViewModel, IDisposable
    {
        private readonly DispatcherTimer _refreshTimer;

        public ObservableCollection<string> ActivityLog { get; } = new();
        public ObservableCollection<LodestoneOption> Lodestones { get; }

        // Live status
        private string _tilePosition = "--";
        private string _exactPosition = "--";
        private bool _isMoving;
        private string _lastStatus = "Ready.";

        // Walk inputs
        private string _targetX = string.Empty;
        private string _targetY = string.Empty;
        private string _targetZ = "0";
        private int _stopShortTiles = 2;
        private int _timeoutMs = 8000;
        private int _jitterTiles = 1;
        private int _nudgeStep = 1;

        // Path inputs
        private string _pathInput = "3200,3200,0\n3205,3210,0";

        // Lodestone inputs
        private LodestoneOption? _selectedLodestone;
        private string _lodestoneSearch = "Varrock";
        private int _teleportTimeoutMs = 12000;

        public string TilePosition { get => _tilePosition; set => SetProperty(ref _tilePosition, value); }
        public string ExactPosition { get => _exactPosition; set => SetProperty(ref _exactPosition, value); }
        public bool IsMoving { get => _isMoving; set => SetProperty(ref _isMoving, value); }
        public string LastStatus { get => _lastStatus; set => SetProperty(ref _lastStatus, value); }

        public string TargetX { get => _targetX; set => SetProperty(ref _targetX, value); }
        public string TargetY { get => _targetY; set => SetProperty(ref _targetY, value); }
        public string TargetZ { get => _targetZ; set => SetProperty(ref _targetZ, value); }
        public int StopShortTiles { get => _stopShortTiles; set => SetProperty(ref _stopShortTiles, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }
        public int JitterTiles { get => _jitterTiles; set => SetProperty(ref _jitterTiles, value); }
        public int NudgeStep { get => _nudgeStep; set => SetProperty(ref _nudgeStep, value); }

        public string PathInput { get => _pathInput; set => SetProperty(ref _pathInput, value); }

        public LodestoneOption? SelectedLodestone { get => _selectedLodestone; set => SetProperty(ref _selectedLodestone, value); }
        public string LodestoneSearch { get => _lodestoneSearch; set => SetProperty(ref _lodestoneSearch, value); }
        public int TeleportTimeoutMs { get => _teleportTimeoutMs; set => SetProperty(ref _teleportTimeoutMs, value); }

        public ICommand RefreshCommand { get; }
        public ICommand WalkToCommand { get; }
        public ICommand WalkPathCommand { get; }
        public ICommand ClickToCommand { get; }
        public ICommand ClickPathCommand { get; }
        public ICommand WaitUntilWithinCommand { get; }
        public ICommand WaitWhileMovingCommand { get; }
        public ICommand TeleportSelectedCommand { get; }
        public ICommand TeleportByNameCommand { get; }
        public ICommand UseCurrentTileCommand { get; }
        public ICommand NudgeXPositiveCommand { get; }
        public ICommand NudgeXNegativeCommand { get; }
        public ICommand NudgeYPositiveCommand { get; }
        public ICommand NudgeYNegativeCommand { get; }
        public ICommand NudgeZPositiveCommand { get; }
        public ICommand NudgeZNegativeCommand { get; }

        public NavigationViewModel()
        {
            Lodestones = new ObservableCollection<LodestoneOption>(BuildLodestones());
            SelectedLodestone = Lodestones.FirstOrDefault();

            RefreshCommand = new RelayCommand(_ => RefreshPosition());
            WalkToCommand = new RelayCommand(_ => WalkTo());
            WalkPathCommand = new RelayCommand(_ => WalkPath());
            ClickToCommand = new RelayCommand(_ => ClickTo());
            ClickPathCommand = new RelayCommand(_ => ClickPath());
            WaitUntilWithinCommand = new RelayCommand(_ => WaitUntilWithin());
            WaitWhileMovingCommand = new RelayCommand(_ => WaitWhileMoving());
            TeleportSelectedCommand = new RelayCommand(_ => TeleportSelected(), _ => SelectedLodestone != null);
            TeleportByNameCommand = new RelayCommand(_ => TeleportByName(), _ => !string.IsNullOrWhiteSpace(LodestoneSearch));
            UseCurrentTileCommand = new RelayCommand(_ => UseCurrentTile());
            NudgeXPositiveCommand = new RelayCommand(_ => TargetX = Adjust(TargetX, _nudgeStep));
            NudgeXNegativeCommand = new RelayCommand(_ => TargetX = Adjust(TargetX, -_nudgeStep));
            NudgeYPositiveCommand = new RelayCommand(_ => TargetY = Adjust(TargetY, _nudgeStep));
            NudgeYNegativeCommand = new RelayCommand(_ => TargetY = Adjust(TargetY, -_nudgeStep));
            NudgeZPositiveCommand = new RelayCommand(_ => TargetZ = Adjust(TargetZ, _nudgeStep));
            NudgeZNegativeCommand = new RelayCommand(_ => TargetZ = Adjust(TargetZ, -_nudgeStep));

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(750)
            };
            _refreshTimer.Tick += OnRefreshTick;
            _refreshTimer.Start();

            RefreshPosition();
            AddLog("Navigation tester ready.");
        }

        private void RefreshPosition()
        {
            try
            {
                var tile = LocalPlayer.GetTilePosition();
                var exact = LocalPlayer.GetExactPosition();
                TilePosition = $"{tile.x}, {tile.y}, {tile.z}";
                ExactPosition = $"{exact.x:0.00}, {exact.y:0.00}, {exact.z:0.00}";
                IsMoving = LocalPlayer.IsMoving();
            }
            catch (Exception ex)
            {
                LastStatus = $"Failed to read player position: {ex.Message}";
            }
        }

        private void WalkTo()
        {
            if (!TryParseTarget(out var x, out var y, out var z))
            {
                LastStatus = "Enter X and Y (ints).";
                return;
            }

            try
            {
                var ok = Traversal.WalkTo(x, y, z, StopShortTiles, TimeoutMs, JitterTiles);
                LastStatus = ok ? $"Walking to {x},{y},{z} (stop {StopShortTiles})." : "WalkTo failed.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"WalkTo error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void WalkPath()
        {
            var points = ParsePath(PathInput).ToArray();
            if (points.Length == 0)
            {
                LastStatus = "Path input is empty or invalid (use x,y,z per line).";
                return;
            }

            try
            {
                var ok = Traversal.WalkPath(points, StopShortTiles, TimeoutMs, JitterTiles);
                LastStatus = ok ? $"Walking path ({points.Length} waypoints)." : "WalkPath failed.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"WalkPath error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void TeleportSelected()
        {
            var selection = SelectedLodestone;
            if (selection == null) return;

            try
            {
                var ok = Traversal.Lodestone(selection.Index, TeleportTimeoutMs);
                LastStatus = ok ? $"Lodestone to {selection.Name} (#{selection.Index}) issued." : $"Lodestone {selection.Name} failed.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"Lodestone error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void ClickTo()
        {
            if (!TryParseTarget(out var x, out var y, out var z))
            {
                LastStatus = "Enter X and Y (ints).";
                return;
            }

            try
            {
                var ok = Traversal.ClickTo(x, y, z, JitterTiles);
                LastStatus = ok ? $"Click issued to {x},{y},{z} (jitter {JitterTiles})." : "ClickTo failed.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"ClickTo error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void ClickPath()
        {
            var points = ParsePath(PathInput).ToArray();
            if (points.Length == 0)
            {
                LastStatus = "Path input is empty or invalid.";
                return;
            }

            try
            {
                var ok = Traversal.ClickPath(points, JitterTiles);
                LastStatus = ok ? $"ClickPath issued ({points.Length} waypoints)." : "ClickPath failed.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"ClickPath error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void WaitUntilWithin()
        {
            if (!TryParseTarget(out var x, out var y, out var z))
            {
                LastStatus = "Enter X and Y (ints).";
                return;
            }

            try
            {
                var ok = Traversal.WaitUntilWithin(x, y, z, StopShortTiles, TimeoutMs);
                LastStatus = ok ? $"Arrived within {StopShortTiles} tiles." : $"Timeout waiting to reach {x},{y},{z}.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"WaitUntilWithin error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void WaitWhileMoving()
        {
            try
            {
                var ok = Traversal.WaitWhileMoving(TimeoutMs);
                LastStatus = ok ? "Stopped moving within timeout." : "Still moving after timeout.";
                AddLog(LastStatus);
            }
            catch (Exception ex)
            {
                LastStatus = $"WaitWhileMoving error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void TeleportByName()
        {
            var dest = LodestoneSearch?.Trim();
            if (string.IsNullOrWhiteSpace(dest))
            {
                LastStatus = "Enter a lodestone name.";
                return;
            }

            try
            {
                var ok = Traversal.Lodestone(dest, TeleportTimeoutMs);
                LastStatus = ok ? $"Lodestone '{dest}' issued." : $"Lodestone '{dest}' failed.";
                AddLog(LastStatus);

                var matched = Lodestones.FirstOrDefault(l => l.Name.IndexOf(dest, StringComparison.OrdinalIgnoreCase) >= 0);
                if (matched != null)
                {
                    SelectedLodestone = matched;
                }
            }
            catch (Exception ex)
            {
                LastStatus = $"Lodestone error: {ex.Message}";
                AddLog(LastStatus);
            }
        }

        private void UseCurrentTile()
        {
            try
            {
                var tile = LocalPlayer.GetTilePosition();
                TargetX = tile.x.ToString();
                TargetY = tile.y.ToString();
                TargetZ = tile.z.ToString();
                LastStatus = $"Target set to current tile {tile.x},{tile.y},{tile.z}.";
            }
            catch (Exception ex)
            {
                LastStatus = $"Failed to read current tile: {ex.Message}";
            }
        }

        private static string Adjust(string current, int delta)
        {
            if (!int.TryParse(current, out var val)) val = 0;
            val += delta;
            return val.ToString();
        }

        private bool TryParseTarget(out int x, out int y, out int z)
        {
            x = y = z = 0;
            if (!int.TryParse(TargetX, out x)) return false;
            if (!int.TryParse(TargetY, out y)) return false;
            if (!int.TryParse(TargetZ, out z)) z = 0;
            return true;
        }

        private IEnumerable<(int x, int y, int z)> ParsePath(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) yield break;

            var segments = input
                .Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);

            foreach (var seg in segments)
            {
                var parts = seg.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                if (!int.TryParse(parts[0], out var x)) continue;
                if (!int.TryParse(parts[1], out var y)) continue;
                int z = 0;
                if (parts.Length >= 3 && int.TryParse(parts[2], out var zVal)) z = zVal;
                yield return (x, y, z);
            }
        }

        private void AddLog(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            ActivityLog.Insert(0, entry);
            while (ActivityLog.Count > 25)
            {
                ActivityLog.RemoveAt(ActivityLog.Count - 1);
            }
        }

        private static IReadOnlyList<LodestoneOption> BuildLodestones()
        {
            return new[]
            {
                new LodestoneOption("Varrock", 0),
                new LodestoneOption("Lumbridge", 1),
                new LodestoneOption("Falador", 2),
                new LodestoneOption("Burthorpe", 3),
                new LodestoneOption("Edgeville", 4),
                new LodestoneOption("Draynor", 5),
                new LodestoneOption("Port Sarim", 6),
                new LodestoneOption("Taverley", 7),
                new LodestoneOption("Al Kharid", 8),
                new LodestoneOption("Fort Forinthry", 9),
                new LodestoneOption("Canifis", 10),
                new LodestoneOption("Wilderness", 11),
                new LodestoneOption("Anachronia", 12),
                new LodestoneOption("Bandit Camp", 13),
                new LodestoneOption("Menaphos", 14),
                new LodestoneOption("Catherby", 15),
                new LodestoneOption("Karamja", 16),
                new LodestoneOption("Fremennik", 17),
                new LodestoneOption("Seers'", 18),
                new LodestoneOption("Ardougne", 19),
                new LodestoneOption("Yanille", 20),
                new LodestoneOption("Oo'glog", 21),
                new LodestoneOption("Lunar Isle", 22),
                new LodestoneOption("Prifddinas", 23),
                new LodestoneOption("Tirannwn", 24),
                new LodestoneOption("Ashdale", 25),
                new LodestoneOption("Eagles' Peak", 28),
            };
        }

        public void Dispose()
        {
            try { _refreshTimer.Stop(); } catch { /* ignored */ }
            _refreshTimer.Tick -= OnRefreshTick;
        }

        private void OnRefreshTick(object? sender, EventArgs e) => RefreshPosition();
    }

    public record LodestoneOption(string Name, int Index)
    {
        public string Display => $"{Name} (#{Index})";
    }
}
