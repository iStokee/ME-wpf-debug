using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    public class MiscApiViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
    {
        private readonly DispatcherTimer _refreshTimer;
        private readonly EventHandler _refreshTickHandler;
        private bool _isActive;

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

        private int _abilityCooldownTimer;
        public int AbilityCooldownTimer
        {
            get => _abilityCooldownTimer;
            set => SetProperty(ref _abilityCooldownTimer, value);
        }

        private bool _abilityFound;
        public bool AbilityFound
        {
            get => _abilityFound;
            set => SetProperty(ref _abilityFound, value);
        }

        private bool _abilityEnabledState;
        public bool AbilityEnabledState
        {
            get => _abilityEnabledState;
            set => SetProperty(ref _abilityEnabledState, value);
        }

        private int _abilityResolvedId;
        public int AbilityResolvedId
        {
            get => _abilityResolvedId;
            set => SetProperty(ref _abilityResolvedId, value);
        }

        private int _abilityResolvedSlot;
        public int AbilityResolvedSlot
        {
            get => _abilityResolvedSlot;
            set => SetProperty(ref _abilityResolvedSlot, value);
        }

        private int _abilityResolvedBar;
        public int AbilityResolvedBar
        {
            get => _abilityResolvedBar;
            set => SetProperty(ref _abilityResolvedBar, value);
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

        private int _buffLookupId = 26033;
        public int BuffLookupId
        {
            get => _buffLookupId;
            set => SetProperty(ref _buffLookupId, value);
        }

        private bool _buffFound;
        public bool BuffFound
        {
            get => _buffFound;
            set => SetProperty(ref _buffFound, value);
        }

        private int _buffDuration;
        public int BuffDuration
        {
            get => _buffDuration;
            set => SetProperty(ref _buffDuration, value);
        }

        private int _buffDurationSeconds;
        public int BuffDurationSeconds
        {
            get => _buffDurationSeconds;
            set => SetProperty(ref _buffDurationSeconds, value);
        }

        private int _debuffLookupId = 14690;
        public int DebuffLookupId
        {
            get => _debuffLookupId;
            set => SetProperty(ref _debuffLookupId, value);
        }

        private bool _debuffFound;
        public bool DebuffFound
        {
            get => _debuffFound;
            set => SetProperty(ref _debuffFound, value);
        }

        private int _debuffDuration;
        public int DebuffDuration
        {
            get => _debuffDuration;
            set => SetProperty(ref _debuffDuration, value);
        }

        private int _debuffDurationSeconds;
        public int DebuffDurationSeconds
        {
            get => _debuffDurationSeconds;
            set => SetProperty(ref _debuffDurationSeconds, value);
        }

        private int _currentTick;
        public int CurrentTick
        {
            get => _currentTick;
            set => SetProperty(ref _currentTick, value);
        }

        private bool _tickBoundaryPassed;
        public bool TickBoundaryPassed
        {
            get => _tickBoundaryPassed;
            set => SetProperty(ref _tickBoundaryPassed, value);
        }

        public sealed class BuffDisplay
        {
            public int Id { get; init; }
            public bool Found { get; init; }
            public int Duration { get; init; }
            public int DurationSeconds { get; init; }
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
        public ICommand RefreshAbilityStateCommand { get; }
        public ICommand QuickHealCommand { get; }
        public ICommand AutoRetaliateCommand { get; }
        public ICommand QuickPrayerCommand { get; }
        public ICommand FamiliarActionCommand { get; }
        public ICommand RefreshBuffsCommand { get; }
        public ICommand DrawRectCommand { get; }
        public ICommand DrawNamedRectCommand { get; }
        public ICommand ClearNamedRectCommand { get; }
        public ICommand LogoutMiniCommand { get; }
        public ICommand LobbyCommand { get; }
        public ICommand ReloadManagedScriptCommand { get; }

        public ObservableCollection<BuffDisplay> ActiveBuffs { get; } = new();
        public ObservableCollection<BuffDisplay> ActiveDebuffs { get; } = new();

        public int ActiveBuffCount => ActiveBuffs.Count;
        public int ActiveDebuffCount => ActiveDebuffs.Count;

        public MiscApiViewModel()
        {
            UseAbilityCommand = new RelayCommand(_ => UseAbility());
            UseAbilityIfReadyCommand = new RelayCommand(_ => UseAbilityIfReady());
            RefreshAbilityStateCommand = new RelayCommand(_ => RefreshAbilityState());
            QuickHealCommand = new RelayCommand(_ => RunBoolAction("ActionButtons.QuickHeal", ActionButtons.QuickHeal));
            AutoRetaliateCommand = new RelayCommand(_ => RunBoolAction("ActionButtons.AutoRetaliate", ActionButtons.AutoRetaliate));
            QuickPrayerCommand = new RelayCommand(_ => RunBoolAction("ActionButtons.QuickPrayer", ActionButtons.QuickPrayer));
            FamiliarActionCommand = new RelayCommand(_ => RunBoolAction($"ActionButtons.Familiar({FamiliarOrder})", () => ActionButtons.Familiar(FamiliarOrder)));
            RefreshBuffsCommand = new RelayCommand(_ => RefreshBuffData());
            DrawRectCommand = new RelayCommand(_ => DrawRect());
            DrawNamedRectCommand = new RelayCommand(_ => DrawNamedRect());
            ClearNamedRectCommand = new RelayCommand(_ => ClearNamedRect());
            LogoutMiniCommand = new RelayCommand(_ => RunBoolAction("Session.LogoutMini", Session.LogoutMini));
            LobbyCommand = new RelayCommand(_ => RunBoolAction("Session.Lobby", Session.Lobby));
            ReloadManagedScriptCommand = new RelayCommand(_ => ReloadManagedScript());

            _refreshTickHandler = OnRefreshTimerTick;
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _refreshTimer.Tick += _refreshTickHandler;
        }

        private void UseAbility()
        {
            var name = AbilityName?.Trim() ?? string.Empty;
            RunBoolAction(
                $"Abilities.Use({name})",
                () => Abilities.Use(name, AbilityActionIndex, Objects.Offsets.GeneralInterfaceRoute, AbilityExactMatch));
            RefreshAbilityState();
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
            RefreshAbilityState();
        }

        private void RefreshAbilityState()
        {
            try
            {
                var state = Abilities.GetState(AbilityName?.Trim() ?? string.Empty, AbilityExactMatch);
                AbilityFound = state.Found;
                AbilityEnabledState = state.Enabled;
                AbilityCooldownTimer = state.CooldownTimer;
                AbilityResolvedId = state.Id;
                AbilityResolvedSlot = state.Slot;
                AbilityResolvedBar = state.BarNumber;
            }
            catch (Exception ex)
            {
                Status = $"Abilities.GetState error: {ex.Message}";
            }
        }

        private void RefreshBuffData()
        {
            try
            {
                CurrentTick = Game.CurrentTick;
                TickBoundaryPassed = Game.CheckTick();

                var buff = BuffBar.GetBuffStatus(BuffLookupId);
                BuffFound = buff.Found;
                BuffDuration = buff.Duration;
                BuffDurationSeconds = buff.DurationSeconds;

                var debuff = BuffBar.GetDebuffStatus(DebuffLookupId);
                DebuffFound = debuff.Found;
                DebuffDuration = debuff.Duration;
                DebuffDurationSeconds = debuff.DurationSeconds;

                ActiveBuffs.Clear();
                foreach (var entry in BuffBar.GetBuffs())
                {
                    ActiveBuffs.Add(new BuffDisplay
                    {
                        Id = entry.Id,
                        Found = entry.Found,
                        Duration = entry.Duration,
                        DurationSeconds = entry.DurationSeconds
                    });
                }

                ActiveDebuffs.Clear();
                foreach (var entry in BuffBar.GetDebuffs())
                {
                    ActiveDebuffs.Add(new BuffDisplay
                    {
                        Id = entry.Id,
                        Found = entry.Found,
                        Duration = entry.Duration,
                        DurationSeconds = entry.DurationSeconds
                    });
                }

                OnPropertyChanged(nameof(ActiveBuffCount));
                OnPropertyChanged(nameof(ActiveDebuffCount));
            }
            catch (Exception ex)
            {
                Status = $"Buff/tick refresh error: {ex.Message}";
            }
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
            _isActive = true;
            RefreshAbilityState();
            RefreshBuffData();
            _refreshTimer.Start();
        }

        public void OnDeactivated()
        {
            _isActive = false;
            _refreshTimer.Stop();
        }

        public void Dispose()
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= _refreshTickHandler;
        }

        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            if (!_isActive)
            {
                return;
            }

            RefreshAbilityState();
            RefreshBuffData();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
