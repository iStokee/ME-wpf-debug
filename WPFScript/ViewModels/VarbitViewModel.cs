using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MESharp.API;
using MESharp.Commands;
using MESharp.ViewModels;

namespace MESharp.ViewModels
{
    public class VarbitEntryViewModel
    {
        public int Id { get; init; }
        public int BaseVar { get; init; }
        public int StartBit { get; init; }
        public int EndBit { get; init; }
        public string Domain { get; init; }
        public bool Loaded { get; init; }
        public int Value { get; init; }
    }

    public class VarbitViewModel : INotifyPropertyChanged, IActivatableViewModel
    {
        private int _varbitId;
        public int VarbitId
        {
            get => _varbitId;
            set
            {
                _varbitId = value;
                OnPropertyChanged(nameof(VarbitId));
            }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private int _varbitValue;
        public int VarbitValue
        {
            get => _varbitValue;
            set
            {
                _varbitValue = value;
                OnPropertyChanged(nameof(VarbitValue));
            }
        }

        private int _varpId;
        public int VarpId
        {
            get => _varpId;
            set
            {
                _varpId = value;
                OnPropertyChanged(nameof(VarpId));
            }
        }

        private int _varpState;
        public int VarpState
        {
            get => _varpState;
            set
            {
                _varpState = value;
                OnPropertyChanged(nameof(VarpState));
            }
        }

        private string _varpBitString;
        public string VarpBitString
        {
            get => _varpBitString;
            set
            {
                _varpBitString = value;
                OnPropertyChanged(nameof(VarpBitString));
            }
        }

        private ulong _varpAddress;
        public ulong VarpAddress
        {
            get => _varpAddress;
            set
            {
                _varpAddress = value;
                OnPropertyChanged(nameof(VarpAddress));
            }
        }

        private ulong _varpIndexAddress;
        public ulong VarpIndexAddress
        {
            get => _varpIndexAddress;
            set
            {
                _varpIndexAddress = value;
                OnPropertyChanged(nameof(VarpIndexAddress));
            }
        }

        public ObservableCollection<VarbitEntryViewModel> VarpVarbits { get; } = new ObservableCollection<VarbitEntryViewModel>();

        public ICommand GetVarbitCommand { get; }
        public ICommand LoadVarpCommand { get; }
        public ICommand ClearVarpCommand { get; }
        public ICommand CopyVarpStateCommand { get; }
        public ICommand CopyVarpBitsCommand { get; }
        public ICommand CopySelectedVarbitIdCommand { get; }

        public VarbitViewModel()
        {
            _status = string.Empty;
            GetVarbitCommand = new RelayCommand(
                new System.Action<object>(GetVarbit),
                new System.Func<object, bool>(_ => Game.IsInjected)
            );
            LoadVarpCommand = new RelayCommand(
                new System.Action<object>(LoadVarp),
                new System.Func<object, bool>(_ => Game.IsInjected)
            );
            ClearVarpCommand = new RelayCommand(
                _ => ClearVarp()
            );
            CopyVarpStateCommand = new RelayCommand(
                _ => CopyText(VarpState.ToString()),
                _ => !string.IsNullOrWhiteSpace(VarpBitString)
            );
            CopyVarpBitsCommand = new RelayCommand(
                _ => CopyText(VarpBitString),
                _ => !string.IsNullOrWhiteSpace(VarpBitString)
            );
            CopySelectedVarbitIdCommand = new RelayCommand(
                _ => CopyText(SelectedVarbit?.Id.ToString() ?? string.Empty),
                _ => SelectedVarbit != null
            );
        }

        private void GetVarbit(object? parameter)
        {
            if (!Game.IsInjected)
            {
                Status = "Error: Not in game.";
                return;
            }
            Status = "Reading...";
            VarbitValue = Game.GetVarbit(VarbitId);
            Status = $"Value of {VarbitId} is {VarbitValue}";
        }

        private void LoadVarp(object? parameter)
        {
            if (!Game.IsInjected)
            {
                Status = "Error: Not in game.";
                return;
            }

            Status = "Reading varp...";
            var varp = Varbits.GetVarp(VarpId);
            VarpState = varp.State;
            VarpAddress = varp.Address;
            VarpIndexAddress = varp.IndexAddress;
            VarpBitString = Varbits.GetVarpBitString(VarpId);

            VarpVarbits.Clear();
            foreach (var bit in Varbits.GetVarbitsFromVarp(VarpId))
            {
                VarpVarbits.Add(new VarbitEntryViewModel
                {
                    Id = bit.Id,
                    BaseVar = bit.BaseVar,
                    StartBit = bit.StartBit,
                    EndBit = bit.EndBit,
                    Domain = GetDomainName(bit.Domain),
                    Loaded = bit.Loaded,
                    Value = Varbits.GetVarbitValue(bit.Id)
                });
            }

            Status = $"Loaded varp {VarpId} with {VarpVarbits.Count} varbit(s).";
        }

        private void ClearVarp()
        {
            VarpVarbits.Clear();
            VarpState = 0;
            VarpBitString = string.Empty;
            VarpAddress = 0;
            VarpIndexAddress = 0;
            SelectedVarbit = null;
        }

        private static string GetDomainName(int domain) => domain switch
        {
            0 => "PLAYER",
            1 => "NPC",
            2 => "CLIENT",
            3 => "WORLD",
            4 => "REGION",
            5 => "OBJECT",
            6 => "CLAN",
            7 => "CLAN SETTING",
            _ => $"UNKNOWN({domain})"
        };

        private VarbitEntryViewModel _selectedVarbit;
        public VarbitEntryViewModel SelectedVarbit
        {
            get => _selectedVarbit;
            set
            {
                _selectedVarbit = value;
                OnPropertyChanged(nameof(SelectedVarbit));
                OnPropertyChanged(nameof(SelectedVarbitLabel));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedVarbitLabel =>
            SelectedVarbit == null ? "(none selected)" : $"{SelectedVarbit.Id} ({SelectedVarbit.Domain})";

        private static void CopyText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }
            Clipboard.SetText(value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnActivated()
        {
            Status = "Enter a varbit ID and click 'Get Value'.";
            VarbitId = 0;
            VarbitValue = 0;
            VarpId = 0;
            ClearVarp();
        }

        public void OnDeactivated()
        {
        }
    }
}
