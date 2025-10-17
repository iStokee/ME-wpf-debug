using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MESharp.API;

namespace MESharp.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged, IDisposable, IActivatableViewModel
    {
        private readonly DispatcherTimer _timer;
        private readonly EventHandler _tickHandler;
        private bool _isActive;
        private bool _disposed;

        private GameState _state;
        public GameState State { get => _state; private set => Set(ref _state, value); }

        private ulong _pid;
        public ulong ProcessId { get => _pid; private set => Set(ref _pid, value); }

        private IntPtr _handle;
        public IntPtr ProcessHandle { get => _handle; private set => Set(ref _handle, value); }

        private IntPtr _hwnd;
        public IntPtr GameWindow { get => _hwnd; private set => Set(ref _hwnd, value); }

        private bool _isInjected;
        public bool IsInjected { get => _isInjected; private set => Set(ref _isInjected, value); }

        private string _localPlayerName = string.Empty;
        public string LocalPlayerName
        {
            get => _localPlayerName;
            private set
            {
                if (Set(ref _localPlayerName, value))
                {
                    OnPropertyChanged(nameof(DisplayedLocalPlayerName));
                }
            }
        }

        private bool _isLocalPlayerNameVisible;
        public bool IsLocalPlayerNameVisible
        {
            get => _isLocalPlayerNameVisible;
            set
            {
                if (Set(ref _isLocalPlayerNameVisible, value))
                {
                    OnPropertyChanged(nameof(DisplayedLocalPlayerName));
                }
            }
        }

        public string DisplayedLocalPlayerName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LocalPlayerName))
                    return "(not available)";

                if (IsLocalPlayerNameVisible)
                    return LocalPlayerName;

                var length = LocalPlayerName.Length;
                if (length < 4) length = 4;
                if (length > 12) length = 12;
                return new string('*', length);
            }
        }

        private string _version = string.Empty;
        public string Version { get => _version; private set => Set(ref _version, value); }

        public GameViewModel()
        {
            _tickHandler = OnTimerTick;
            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1),
                IsEnabled = false
            };
            _timer.Tick += _tickHandler;

            // Defer OnActivated to allow window to show first (improves startup time)
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                OnActivated();
            }), DispatcherPriority.Background);
        }

        private void OnTimerTick(object? sender, EventArgs e) => Refresh();

        private void Refresh()
        {
            if (_disposed)
                return;

            try
            {
                State = Game.State;
                ProcessId = Game.ProcessId;
                ProcessHandle = Game.ProcessHandle;
                GameWindow = Game.GameWindow;
                IsInjected = Game.IsInjected;
                LocalPlayerName = Game.LocalPlayerName;
                Version = Game.Version;
            }
            catch { /* ignore */ }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (!Equals(f, v)) { f = v; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n)); return true; }
            return false;
        }

        private void OnPropertyChanged(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnActivated()
        {
            if (_disposed)
                return;

            if (_isActive)
            {
                Refresh();
                return;
            }

            _isActive = true;
            if (!_timer.IsEnabled)
            {
                try { _timer.Start(); } catch { /* ignore */ }
            }

            Refresh();
        }

        public void OnDeactivated()
        {
            if (_disposed || !_isActive)
                return;

            _isActive = false;
            try { _timer.Stop(); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            OnDeactivated();

            _disposed = true;
            try
            {
                _timer.Tick -= _tickHandler;
            }
            catch { /* ignore */ }
        }
    }
}
