using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    public class ObjectsViewModel : INotifyPropertyChanged, IDisposable, IActivatableViewModel
    {
        private const int DefaultResultLimit = 400;
        private static readonly TimeSpan LiveRefreshInterval = TimeSpan.FromSeconds(1);

        private readonly DispatcherTimer _timer;
        private bool _disposed;
        private bool _isActive;

        public ObservableCollection<Objects.GameObject> AllObjects { get; } = new();
        public ICollectionView ObjectsView { get; }

        public IReadOnlyList<int> ActionIndices { get; } = Enumerable.Range(0, 11).ToArray();

        private Objects.GameObject? _selectedObject;
        public Objects.GameObject? SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (Set(ref _selectedObject, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (Set(ref _filterText, value))
                {
                    ObjectsView.Refresh();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private bool _includeObject;
        public bool IncludeObject
        {
            get => _includeObject;
            set => SetTypeFlag(ref _includeObject, value);
        }

        private bool _includeNpc;
        public bool IncludeNpc
        {
            get => _includeNpc;
            set => SetTypeFlag(ref _includeNpc, value);
        }

        private bool _includePlayer;
        public bool IncludePlayer
        {
            get => _includePlayer;
            set => SetTypeFlag(ref _includePlayer, value);
        }

        private bool _includeGroundItem;
        public bool IncludeGroundItem
        {
            get => _includeGroundItem;
            set => SetTypeFlag(ref _includeGroundItem, value);
        }

        private bool _includeHighlight;
        public bool IncludeHighlight
        {
            get => _includeHighlight;
            set => SetTypeFlag(ref _includeHighlight, value);
        }

        private bool _includeProjectile;
        public bool IncludeProjectile
        {
            get => _includeProjectile;
            set => SetTypeFlag(ref _includeProjectile, value);
        }

        private bool _includeTile;
        public bool IncludeTile
        {
            get => _includeTile;
            set => SetTypeFlag(ref _includeTile, value);
        }

        private bool _includeObject12;
        public bool IncludeObject12
        {
            get => _includeObject12;
            set => SetTypeFlag(ref _includeObject12, value);
        }

        private bool _onlyInteractable;
        public bool OnlyInteractable
        {
            get => _onlyInteractable;
            set
            {
                if (Set(ref _onlyInteractable, value))
                {
                    RefreshAndUpdateTimer();
                }
            }
        }

        private bool _liveRefresh;
        public bool LiveRefresh
        {
            get => _liveRefresh;
            set
            {
                if (Set(ref _liveRefresh, value))
                {
                    UpdateTimer();
                    if (value && HasTypeSelection)
                    {
                        Refresh();
                    }
                }
            }
        }

        private int _actionIndex;
        public int ActionIndex
        {
            get => _actionIndex;
            set => Set(ref _actionIndex, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (Set(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool HasTypeSelection =>
            IncludeObject || IncludeNpc || IncludePlayer || IncludeGroundItem ||
            IncludeHighlight || IncludeProjectile || IncludeTile || IncludeObject12;

        public bool HasObjects => AllObjects.Count > 0;

        public ICommand RefreshCommand { get; }
        public ICommand DoActionCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObjectsViewModel()
        {
            ObjectsView = CollectionViewSource.GetDefaultView(AllObjects);
            ObjectsView.Filter = FilterPredicate;
            ObjectsView.SortDescriptions.Add(new SortDescription(nameof(Objects.GameObject.Distance), ListSortDirection.Ascending));

            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = LiveRefreshInterval,
                IsEnabled = false
            };
            _timer.Tick += OnTimerTick;

            RefreshCommand = new RelayCommand(_ => Refresh(), _ => CanRefresh());
            DoActionCommand = new RelayCommand(_ => DoAction(), _ => CanDoAction());
        }

        private void SetTypeFlag(ref bool field, bool value)
        {
            if (Set(ref field, value))
            {
                OnPropertyChanged(nameof(HasTypeSelection));
                RefreshAndUpdateTimer();
            }
        }

        private void RefreshAndUpdateTimer()
        {
            UpdateTimer();
            Refresh();
        }

        private void UpdateTimer()
        {
            if (_disposed)
                return;

            var shouldRun = _isActive && _liveRefresh && HasTypeSelection;

            if (shouldRun)
            {
                if (!_timer.IsEnabled)
                {
                    _timer.Start();
                }
            }
            else if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_disposed)
                return;

            Refresh();
        }

        private bool CanRefresh() => HasTypeSelection && !IsBusy;

        private bool CanDoAction()
            => !string.IsNullOrWhiteSpace(FilterText) || SelectedObject != null;

        private void Refresh()
        {
            if (_disposed || IsBusy)
                return;

            if (!HasTypeSelection)
            {
                SelectedObject = null;
                AllObjects.Clear();
                ObjectsView.Refresh();
                return;
            }

            try
            {
                IsBusy = true;

                var selectedTypes = GetSelectedTypeCodes();
                var previous = SelectedObject;
                SelectedObject = null;

                var snapshot = Objects.GetAll();
                var filtered = snapshot
                    .Where(obj => selectedTypes.Contains(obj.Type))
                    .Where(obj => !_onlyInteractable || !string.IsNullOrWhiteSpace(obj.Action))
                    .OrderBy(obj => obj.Distance)
                    .Take(DefaultResultLimit)
                    .ToList();

                AllObjects.Clear();
                foreach (var obj in filtered)
                {
                    AllObjects.Add(obj);
                }

                ObjectsView.Refresh();
                OnPropertyChanged(nameof(HasObjects));

                if (previous != null)
                {
                    var restored = AllObjects.FirstOrDefault(o =>
                        o.Id == previous.Id &&
                        o.Type == previous.Type &&
                        string.Equals(o.Name, previous.Name, StringComparison.OrdinalIgnoreCase));

                    if (restored != null)
                    {
                        SelectedObject = restored;
                    }
                }
            }
            catch
            {
                // Native layer may not be initialised; ignore and keep current state.
            }
            finally
            {
                IsBusy = false;
            }
        }

        private HashSet<int> GetSelectedTypeCodes()
        {
            var codes = new HashSet<int>();
            if (IncludeObject) codes.Add((int)Objects.ObjectKind.Object);
            if (IncludeNpc) codes.Add((int)Objects.ObjectKind.Npc);
            if (IncludePlayer) codes.Add((int)Objects.ObjectKind.Player);
            if (IncludeGroundItem) codes.Add((int)Objects.ObjectKind.GroundItem);
            if (IncludeHighlight) codes.Add((int)Objects.ObjectKind.Highlight);
            if (IncludeProjectile) codes.Add((int)Objects.ObjectKind.Projectile);
            if (IncludeTile) codes.Add((int)Objects.ObjectKind.Tile);
            if (IncludeObject12) codes.Add((int)Objects.ObjectKind.Object12);
            return codes;
        }

        private bool FilterPredicate(object obj)
        {
            if (string.IsNullOrWhiteSpace(FilterText))
                return true;

            if (obj is not Objects.GameObject go)
                return false;

            var token = FilterText.Trim();
            if (string.IsNullOrEmpty(token))
                return true;

            token = token.Replace("*", string.Empty);
            if (token.Length == 0)
                return true;

            if (int.TryParse(token, out var id))
                return go.Id == id;

            var comparison = StringComparison.OrdinalIgnoreCase;
            if (!string.IsNullOrEmpty(go.Name) && go.Name.IndexOf(token, comparison) >= 0)
                return true;

            if (!string.IsNullOrEmpty(go.Action) && go.Action.IndexOf(token, comparison) >= 0)
                return true;

            return false;
        }

        private void DoAction()
        {
            bool ok = false;

            var token = FilterText?.Trim() ?? string.Empty;
            token = token.Replace("*", string.Empty);

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (int.TryParse(token, out var id))
                {
                    ok = Objects.DoActionByIds(new[] { id }, ActionIndex);
                }
                else
                {
                    ok = Objects.DoActionByNames(new[] { token }, ActionIndex);
                }
            }
            else if (SelectedObject != null)
            {
                ok = SelectedObject.DoAction(ActionIndex);
            }

            // optional: surface `ok` to UI in the future
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return true;
            }

            return false;
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose()
        {
            if (_disposed)
                return;

            OnDeactivated();

            _disposed = true;

            try
            {
                _timer.Tick -= OnTimerTick;
            }
            catch { /* ignore */ }
        }

        public void OnActivated()
        {
            if (_disposed)
                return;

            if (_isActive)
            {
                UpdateTimer();
                if (HasTypeSelection)
                {
                    Refresh();
                }
                return;
            }

            _isActive = true;
            UpdateTimer();

            if (HasTypeSelection)
            {
                Refresh();
            }
        }

        public void OnDeactivated()
        {
            if (_disposed || !_isActive)
                return;

            _isActive = false;
            try { _timer.Stop(); } catch { /* ignore */ }
        }
    }
}
