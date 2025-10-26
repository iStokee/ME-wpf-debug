using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MESharp.API;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class PlayersViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
    {
        private readonly DispatcherTimer _refreshTimer;
        private bool _autoRefresh;
        private string _filterText = "";
        private int _maxDistance = 50;
        private string _statusMessage = "Ready";
        private Players.Player _selectedPlayer;

        public PlayersViewModel()
        {
            Players = new ObservableCollection<Players.Player>();
            PlayersView = CollectionViewSource.GetDefaultView(Players);
            PlayersView.Filter = FilterPlayers;

            LoadPlayersCommand = new RelayCommand(_ => LoadPlayers());
            ClearCommand = new RelayCommand(_ => Clear());
            AttackCommand = new RelayCommand(_ => DoAttack(), _ => SelectedPlayer != null);
            FollowCommand = new RelayCommand(_ => DoFollow(), _ => SelectedPlayer != null);
            TradeCommand = new RelayCommand(_ => DoTrade(), _ => SelectedPlayer != null);
            ExamineCommand = new RelayCommand(_ => DoExamine(), _ => SelectedPlayer != null);

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _refreshTimer.Tick += (s, e) => RefreshLocalPlayerAndPlayers();
        }

        #region Properties

        public ObservableCollection<Players.Player> Players { get; }
        public ICollectionView PlayersView { get; }

        public Players.Player SelectedPlayer
        {
            get => _selectedPlayer;
            set { _selectedPlayer = value; OnPropertyChanged(); }
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                _autoRefresh = value;
                if (_autoRefresh) _refreshTimer.Start();
                else _refreshTimer.Stop();
                OnPropertyChanged();
            }
        }

        public string FilterText
        {
            get => _filterText;
            set { _filterText = value; OnPropertyChanged(); PlayersView.Refresh(); }
        }

        public int MaxDistance
        {
            get => _maxDistance;
            set { _maxDistance = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int PlayerCount => Players.Count;
        public bool HasPlayers => Players.Count > 0;

        // LocalPlayer Properties
        private bool _isLoggedIn;
        private int _tileX, _tileY, _tileZ;
        private float _exactX, _exactY, _exactZ;
        private bool _isMoving;
        private int _animation;
        private bool _isInCombat;
        private int _hoverProgress;
        private string _interactingWith = "";
        private int _interactingWithId;

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set { _isLoggedIn = value; OnPropertyChanged(); }
        }

        public string TilePosition => $"({_tileX}, {_tileY}, {_tileZ})";
        public string ExactPosition => $"({_exactX:F2}, {_exactY:F2}, {_exactZ:F2})";

        public bool IsMoving
        {
            get => _isMoving;
            set { _isMoving = value; OnPropertyChanged(); }
        }

        public int Animation
        {
            get => _animation;
            set { _animation = value; OnPropertyChanged(); }
        }

        public bool IsInCombat
        {
            get => _isInCombat;
            set { _isInCombat = value; OnPropertyChanged(); }
        }

        public int HoverProgress
        {
            get => _hoverProgress;
            set { _hoverProgress = value; OnPropertyChanged(); }
        }

        public string InteractingWith
        {
            get => _interactingWith;
            set { _interactingWith = value; OnPropertyChanged(); }
        }

        public int InteractingWithId
        {
            get => _interactingWithId;
            set { _interactingWithId = value; OnPropertyChanged(); }
        }

        // Distance Calculator
        private int _distanceToX, _distanceToY, _distanceToZ;
        private float _calculatedDistance;

        public int DistanceToX
        {
            get => _distanceToX;
            set { _distanceToX = value; OnPropertyChanged(); CalculateDistance(); }
        }

        public int DistanceToY
        {
            get => _distanceToY;
            set { _distanceToY = value; OnPropertyChanged(); CalculateDistance(); }
        }

        public int DistanceToZ
        {
            get => _distanceToZ;
            set { _distanceToZ = value; OnPropertyChanged(); CalculateDistance(); }
        }

        public float CalculatedDistance
        {
            get => _calculatedDistance;
            private set { _calculatedDistance = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand LoadPlayersCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand AttackCommand { get; }
        public ICommand FollowCommand { get; }
        public ICommand TradeCommand { get; }
        public ICommand ExamineCommand { get; }

        #endregion

        #region Methods

        private bool FilterPlayers(object obj)
        {
            if (obj is not Players.Player player) return false;
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            return player.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }

        private void LoadPlayers()
        {
            try
            {
                var allPlayers = MESharp.API.Players.GetAll()
                    .Where(p => p.Distance <= MaxDistance)
                    .OrderBy(p => p.Distance)
                    .ToList();

                Players.Clear();
                foreach (var player in allPlayers)
                {
                    Players.Add(player);
                }

                StatusMessage = $"Loaded {Players.Count} player(s) within {MaxDistance} tiles";
                OnPropertyChanged(nameof(PlayerCount));
                OnPropertyChanged(nameof(HasPlayers));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading players: {ex.Message}";
            }
        }

        private void RefreshLocalPlayerAndPlayers()
        {
            RefreshLocalPlayer();
            if (AutoRefresh)
            {
                LoadPlayers();
            }
        }

        private void RefreshLocalPlayer()
        {
            try
            {
                IsLoggedIn = LocalPlayer.IsLoggedIn();

                var tilePos = LocalPlayer.GetTilePosition();
                _tileX = tilePos.x;
                _tileY = tilePos.y;
                _tileZ = tilePos.z;
                OnPropertyChanged(nameof(TilePosition));

                var exactPos = LocalPlayer.GetExactPosition();
                _exactX = exactPos.x;
                _exactY = exactPos.y;
                _exactZ = exactPos.z;
                OnPropertyChanged(nameof(ExactPosition));

                IsMoving = LocalPlayer.IsMoving();
                Animation = LocalPlayer.GetAnimation();
                IsInCombat = LocalPlayer.IsInCombat();
                HoverProgress = LocalPlayer.GetHoverProgress();
                InteractingWith = LocalPlayer.GetInteractingWith();
                InteractingWithId = LocalPlayer.GetInteractingWithId();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing LocalPlayer: {ex.Message}";
            }
        }

        private void CalculateDistance()
        {
            try
            {
                CalculatedDistance = LocalPlayer.DistanceTo(_distanceToX, _distanceToY, _distanceToZ);
            }
            catch
            {
                CalculatedDistance = 0;
            }
        }

        private void Clear()
        {
            Players.Clear();
            StatusMessage = "Cleared";
            OnPropertyChanged(nameof(PlayerCount));
            OnPropertyChanged(nameof(HasPlayers));
        }

        private void DoAttack()
        {
            if (SelectedPlayer == null) return;
            try
            {
                bool success = SelectedPlayer.Attack(MaxDistance);
                StatusMessage = success ? $"Attacking {SelectedPlayer.Name}" : "Attack failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void DoFollow()
        {
            if (SelectedPlayer == null) return;
            try
            {
                bool success = SelectedPlayer.Follow(MaxDistance);
                StatusMessage = success ? $"Following {SelectedPlayer.Name}" : "Follow failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void DoTrade()
        {
            if (SelectedPlayer == null) return;
            try
            {
                bool success = SelectedPlayer.Trade(MaxDistance);
                StatusMessage = success ? $"Trading with {SelectedPlayer.Name}" : "Trade failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void DoExamine()
        {
            if (SelectedPlayer == null) return;
            try
            {
                bool success = SelectedPlayer.Examine(MaxDistance);
                StatusMessage = success ? $"Examining {SelectedPlayer.Name}" : "Examine failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        #endregion

        #region IActivatableViewModel

        public void OnActivated()
        {
            RefreshLocalPlayer();
            if (AutoRefresh) _refreshTimer.Start();
        }

        public void OnDeactivated()
        {
            _refreshTimer.Stop();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _refreshTimer?.Stop();
        }

        #endregion
    }
}
