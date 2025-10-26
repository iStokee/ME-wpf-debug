using MESharp.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public string Text => $"{Component.TextIds} {Component.TextItem}";


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class InterfacesViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
    {
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

        public bool HasInterfaces => AllInterfaces.Count > 0;
        
        public ICommand LoadInterfacesCommand { get; }
        public ICommand ClearCommand { get; }

        public InterfacesViewModel()
        {
            LoadInterfacesCommand = new RelayCommand(_ => LoadInterfaces());
            ClearCommand = new RelayCommand(_ =>
            {
                AllInterfaces.Clear();
                InterfaceCount = 0;
                StatusMessage = "Cleared.";
                OnPropertyChanged(nameof(HasInterfaces));
            });

            _updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += OnTimerTick;

            StatusMessage = "Click Load Interfaces to scan the game UI.";
        }

        private void LoadInterfaces()
        {
            try
            {
                AllInterfaces.Clear();

                var components = Interfaces.GetAll();
                if (components.Count == 0)
                {
                    StatusMessage = "No interfaces found.";
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
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
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
}
