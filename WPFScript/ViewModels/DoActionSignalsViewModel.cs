using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    public sealed class DoActionSignalRow
    {
        public DateTime TimestampUtc { get; init; }
        public string Surface { get; init; } = string.Empty;
        public string Operation { get; init; } = string.Empty;
        public bool Result { get; init; }
        public string Snippet { get; init; } = string.Empty;
        public string Notes { get; init; } = string.Empty;
    }

    public sealed class DoActionSignalsViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
    {
        private readonly DispatcherTimer _timer;
        private bool _isActive;
        private bool _disposed;
        private int _maxCount = 100;
        private bool _includeFailed = true;
        private bool _autoRefresh = true;
        private bool _captureEnabled = true;
        private bool _echoToConsole;
        private string _statusMessage = "Ready.";
        private DoActionSignalRow? _selectedSignal;

        public ObservableCollection<DoActionSignalRow> Signals { get; } = new();

        public int MaxCount
        {
            get => _maxCount;
            set => SetProperty(ref _maxCount, Math.Clamp(value, 10, 500));
        }

        public bool IncludeFailed
        {
            get => _includeFailed;
            set => SetProperty(ref _includeFailed, value);
        }

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

        public bool CaptureEnabled
        {
            get => _captureEnabled;
            set
            {
                if (SetProperty(ref _captureEnabled, value))
                {
                    DoActionDebugSignals.Configure(enabled: value);
                    StatusMessage = value ? "DoAction capture enabled." : "DoAction capture disabled.";
                    if (value)
                    {
                        RefreshSignals();
                    }
                }
            }
        }

        public bool EchoToConsole
        {
            get => _echoToConsole;
            set
            {
                if (SetProperty(ref _echoToConsole, value))
                {
                    DoActionDebugSignals.Configure(echoToConsole: value);
                    StatusMessage = value ? "DoAction signal console echo enabled." : "DoAction signal console echo disabled.";
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DoActionSignalRow? SelectedSignal
        {
            get => _selectedSignal;
            set
            {
                if (SetProperty(ref _selectedSignal, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearBufferCommand { get; }
        public ICommand CopySelectedCommand { get; }
        public ICommand CopyAllCommand { get; }

        public DoActionSignalsViewModel()
        {
            RefreshCommand = new RelayCommand(_ => RefreshSignals());
            ClearBufferCommand = new RelayCommand(_ => ClearBuffer());
            CopySelectedCommand = new RelayCommand(_ => CopySelected(), _ => SelectedSignal != null);
            CopyAllCommand = new RelayCommand(_ => CopyAll(), _ => Signals.Count > 0);

            var config = DoActionDebugSignals.GetConfig();
            _captureEnabled = config.Enabled;
            _echoToConsole = config.EchoToConsole;

            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(750)
            };
            _timer.Tick += (_, _) =>
            {
                if (_isActive && AutoRefresh)
                {
                    RefreshSignals();
                }
            };

            RefreshSignals();
        }

        public void OnActivated()
        {
            _isActive = true;
            UpdateTimer();
            RefreshSignals();
        }

        public void OnDeactivated()
        {
            _isActive = false;
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (_disposed)
            {
                return;
            }

            if (_isActive && AutoRefresh)
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

        private void RefreshSignals()
        {
            try
            {
                var snapshot = DoActionDebugSignals.Snapshot(MaxCount, IncludeFailed);
                Signals.Clear();
                foreach (var signal in snapshot)
                {
                    Signals.Add(new DoActionSignalRow
                    {
                        TimestampUtc = signal.TimestampUtc,
                        Surface = signal.Surface,
                        Operation = signal.Operation,
                        Result = signal.Result,
                        Snippet = signal.Snippet,
                        Notes = signal.Notes
                    });
                }

                StatusMessage = $"Loaded {Signals.Count} signal(s).";
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Refresh failed: {ex.Message}";
            }
        }

        private void ClearBuffer()
        {
            try
            {
                var removed = DoActionDebugSignals.Clear();
                RefreshSignals();
                StatusMessage = $"Cleared {removed} buffered signal(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Clear failed: {ex.Message}";
            }
        }

        private void CopySelected()
        {
            if (SelectedSignal == null)
            {
                return;
            }

            var text = $"[{SelectedSignal.TimestampUtc:O}] {SelectedSignal.Surface}.{SelectedSignal.Operation} => {(SelectedSignal.Result ? "OK" : "FAILED")}{Environment.NewLine}{SelectedSignal.Snippet}";
            Clipboard.SetText(text);
            StatusMessage = "Copied selected signal.";
        }

        private void CopyAll()
        {
            var lines = Signals.Select(s =>
                $"[{s.TimestampUtc:O}] {s.Surface}.{s.Operation} => {(s.Result ? "OK" : "FAILED")} | {s.Snippet}");
            Clipboard.SetText(string.Join(Environment.NewLine, lines));
            StatusMessage = $"Copied {Signals.Count} signals.";
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _timer.Stop();
            _disposed = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
