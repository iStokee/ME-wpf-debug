using System.ComponentModel;
using System.Windows.Input;
using MESharp.API;
using MESharp.Commands;
using MESharp.ViewModels;

namespace MESharp.ViewModels
{
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

        public ICommand GetVarbitCommand { get; }

        public VarbitViewModel()
        {
            _status = string.Empty;
            GetVarbitCommand = new RelayCommand(
                (Action<object>)GetVarbit,
                (Func<object, bool>)(_ => Game.IsInjected)
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
        }

        public void OnDeactivated()
        {
        }
    }
}
