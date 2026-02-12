using MESharp.API;
using MESharp.Commands;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public class MiscApiViewModel : INotifyPropertyChanged, IActivatableViewModel
    {
        private string _abilityName = "Slice";
        public string AbilityName
        {
            get => _abilityName;
            set => SetProperty(ref _abilityName, value);
        }

        private int _abilityActionIndex;
        public int AbilityActionIndex
        {
            get => _abilityActionIndex;
            set => SetProperty(ref _abilityActionIndex, value);
        }

        private bool _abilityExactMatch;
        public bool AbilityExactMatch
        {
            get => _abilityExactMatch;
            set => SetProperty(ref _abilityExactMatch, value);
        }

        private bool _abilityRequireEnabled = true;
        public bool AbilityRequireEnabled
        {
            get => _abilityRequireEnabled;
            set => SetProperty(ref _abilityRequireEnabled, value);
        }

        private bool _abilityRequireNotOnCooldown = true;
        public bool AbilityRequireNotOnCooldown
        {
            get => _abilityRequireNotOnCooldown;
            set => SetProperty(ref _abilityRequireNotOnCooldown, value);
        }

        private int _familiarOrder = 1;
        public int FamiliarOrder
        {
            get => _familiarOrder;
            set => SetProperty(ref _familiarOrder, value);
        }

        private int _rectX = 100;
        public int RectX
        {
            get => _rectX;
            set => SetProperty(ref _rectX, value);
        }

        private int _rectY = 100;
        public int RectY
        {
            get => _rectY;
            set => SetProperty(ref _rectY, value);
        }

        private int _rectWidth = 180;
        public int RectWidth
        {
            get => _rectWidth;
            set => SetProperty(ref _rectWidth, value);
        }

        private int _rectHeight = 80;
        public int RectHeight
        {
            get => _rectHeight;
            set => SetProperty(ref _rectHeight, value);
        }

        private int _rectDurationMs = 1000;
        public int RectDurationMs
        {
            get => _rectDurationMs;
            set => SetProperty(ref _rectDurationMs, value);
        }

        private float _rectThickness = 2.0f;
        public float RectThickness
        {
            get => _rectThickness;
            set => SetProperty(ref _rectThickness, value);
        }

        private bool _rectFilled;
        public bool RectFilled
        {
            get => _rectFilled;
            set => SetProperty(ref _rectFilled, value);
        }

        private string _namedRectId = "misc-highlight";
        public string NamedRectId
        {
            get => _namedRectId;
            set => SetProperty(ref _namedRectId, value);
        }

        private bool _namedRectPermanent;
        public bool NamedRectPermanent
        {
            get => _namedRectPermanent;
            set => SetProperty(ref _namedRectPermanent, value);
        }

        private string _reloadScriptPath = string.Empty;
        public string ReloadScriptPath
        {
            get => _reloadScriptPath;
            set => SetProperty(ref _reloadScriptPath, value);
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ICommand UseAbilityCommand { get; }
        public ICommand UseAbilityIfReadyCommand { get; }
        public ICommand QuickHealCommand { get; }
        public ICommand AutoRetaliateCommand { get; }
        public ICommand QuickPrayerCommand { get; }
        public ICommand FamiliarActionCommand { get; }
        public ICommand DrawRectCommand { get; }
        public ICommand DrawNamedRectCommand { get; }
        public ICommand ClearNamedRectCommand { get; }
        public ICommand LogoutMiniCommand { get; }
        public ICommand LobbyCommand { get; }
        public ICommand ReloadManagedScriptCommand { get; }

        public MiscApiViewModel()
        {
            UseAbilityCommand = new RelayCommand(_ => UseAbility());
            UseAbilityIfReadyCommand = new RelayCommand(_ => UseAbilityIfReady());
            QuickHealCommand = new RelayCommand(_ => RunBoolAction("ActionButtons.QuickHeal", ActionButtons.QuickHeal));
            AutoRetaliateCommand = new RelayCommand(_ => RunBoolAction("ActionButtons.AutoRetaliate", ActionButtons.AutoRetaliate));
            QuickPrayerCommand = new RelayCommand(_ => RunBoolAction("ActionButtons.QuickPrayer", ActionButtons.QuickPrayer));
            FamiliarActionCommand = new RelayCommand(_ => RunBoolAction($"ActionButtons.Familiar({FamiliarOrder})", () => ActionButtons.Familiar(FamiliarOrder)));
            DrawRectCommand = new RelayCommand(_ => DrawRect());
            DrawNamedRectCommand = new RelayCommand(_ => DrawNamedRect());
            ClearNamedRectCommand = new RelayCommand(_ => ClearNamedRect());
            LogoutMiniCommand = new RelayCommand(_ => RunBoolAction("Session.LogoutMini", Session.LogoutMini));
            LobbyCommand = new RelayCommand(_ => RunBoolAction("Session.Lobby", Session.Lobby));
            ReloadManagedScriptCommand = new RelayCommand(_ => ReloadManagedScript());
        }

        private void UseAbility()
        {
            var name = AbilityName?.Trim() ?? string.Empty;
            RunBoolAction(
                $"Abilities.Use({name})",
                () => Abilities.Use(name, AbilityActionIndex, Objects.Offsets.GeneralInterfaceRoute, AbilityExactMatch));
        }

        private void UseAbilityIfReady()
        {
            var name = AbilityName?.Trim() ?? string.Empty;
            RunBoolAction(
                $"Abilities.UseIfReady({name})",
                () => Abilities.UseIfReady(
                    name,
                    AbilityActionIndex,
                    Objects.Offsets.GeneralInterfaceRoute,
                    AbilityRequireEnabled,
                    AbilityRequireNotOnCooldown,
                    AbilityExactMatch));
        }

        private void DrawRect()
        {
            RunBoolAction(
                $"DebugDraw.HighlightRect({RectX},{RectY},{RectWidth},{RectHeight})",
                () => DebugDraw.HighlightRect(RectX, RectY, RectWidth, RectHeight, RectDurationMs, RectThickness, RectFilled));
        }

        private void DrawNamedRect()
        {
            var id = NamedRectId?.Trim() ?? string.Empty;
            RunBoolAction(
                $"DebugDraw.HighlightRect('{id}', ...)",
                () => DebugDraw.HighlightRect(id, RectX, RectY, RectWidth, RectHeight, 0, 255, 128, 180, RectDurationMs, RectThickness, RectFilled, NamedRectPermanent));
        }

        private void ClearNamedRect()
        {
            try
            {
                var id = NamedRectId?.Trim() ?? string.Empty;
                DebugDraw.Clear(id);
                Status = $"DebugDraw.Clear('{id}') sent.";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private void ReloadManagedScript()
        {
            try
            {
                ScriptHost.ReloadManagedScript(ReloadScriptPath);
                Status = $"ScriptHost.ReloadManagedScript('{ReloadScriptPath}') sent.";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private void RunBoolAction(string label, Func<bool> action)
        {
            try
            {
                var ok = action();
                Status = ok ? $"{label}: OK" : $"{label}: returned false";
            }
            catch (Exception ex)
            {
                Status = $"{label}: Error - {ex.Message}";
            }
        }

        public void OnActivated()
        {
            Status = "Use this page for APIs that don't have dedicated tabs.";
        }

        public void OnDeactivated()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
