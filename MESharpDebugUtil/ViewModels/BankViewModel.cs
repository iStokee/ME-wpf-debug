using MESharp.API;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class BankViewModel : INotifyPropertyChanged
    {
        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => Set(ref _isOpen, value); }

        // Quick filter (id or name) shows current stack size
        private string _quickFilterText;
        private int _quickStack;
        public string QuickFilterText { get => _quickFilterText; set { if (Set(ref _quickFilterText, value)) UpdateQuickFilter(); } }
        public int QuickStack { get => _quickStack; set { if (Set(ref _quickStack, value)) OnPropertyChanged(nameof(HasStack)); } }

		public bool HasStack => QuickStack > 0;

        private string _idsInput, _namesInput, _idInput, _nameInput;
        public string IdsInput { get => _idsInput; set => Set(ref _idsInput, value); }
        public string NamesInput { get => _namesInput; set => Set(ref _namesInput, value); }
        public string IdInput { get => _idInput; set => Set(ref _idInput, value); }
        public string NameInput { get => _nameInput; set => Set(ref _nameInput, value); }
        private string _actionResult, _stackResult;
        public string ActionResult { get => _actionResult; set => Set(ref _actionResult, value); }
        public string StackResult { get => _stackResult; set => Set(ref _stackResult, value); }

        private string _pinInput;
        public string PinInput { get => _pinInput; set => Set(ref _pinInput, value); }

        private string _presetNumberInput;
        public string PresetNumberInput { get => _presetNumberInput; set => Set(ref _presetNumberInput, value); }

        private string _presetSettingsNumberInput;
        public string PresetSettingsNumberInput { get => _presetSettingsNumberInput; set => Set(ref _presetSettingsNumberInput, value); }

        private string _xQuantityInput;
        public string XQuantityInput { get => _xQuantityInput; set => Set(ref _xQuantityInput, value); }

        private bool _noteModeEnabled;
        public bool NoteModeEnabled { get => _noteModeEnabled; set => Set(ref _noteModeEnabled, value); }

        public ICommand RefreshStateCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand LoadLastPresetCommand { get; }
        public ICommand EnterPinCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand DepositAllCommand { get; }
        public ICommand DepositAllExceptIdsCommand { get; }
        public ICommand DepositAllExceptNamesCommand { get; }
        public ICommand DepositInventoryCommand { get; }
        public ICommand DepositEquipmentCommand { get; }
        public ICommand DepositSummonCommand { get; }
        public ICommand DepositMoneyPouchCommand { get; }

        public ICommand OpenInventoryTabCommand { get; }
        public ICommand OpenBoBTabCommand { get; }
        public ICommand OpenEquipmentTabCommand { get; }
        public ICommand SetTransferTabCommand { get; }
        public ICommand SetPresetTabCommand { get; }

        public ICommand SetQuantity1Command { get; }
        public ICommand SetQuantity5Command { get; }
        public ICommand SetQuantity10Command { get; }
        public ICommand SetQuantityXCommand { get; }
        public ICommand SetQuantityAllCommand { get; }
        public ICommand SetXQuantityCommand { get; }
        public ICommand ToggleNoteModeCommand { get; }

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand SaveSummonPresetCommand { get; }
        public ICommand LoadSummonPresetCommand { get; }

        public ICommand PresetSettingsOpenCommand { get; }
        public ICommand PresetSettingsReturnCommand { get; }
        public ICommand PresetSettingsSelectCommand { get; }

        public ICommand DepositBoxOpenCommand { get; }
        public ICommand DepositBoxCloseCommand { get; }
        public ICommand DepositBoxDepositInventoryCommand { get; }
        public ICommand DepositBoxDepositEquipmentCommand { get; }
        public ICommand DepositBoxDepositMoneyCommand { get; }
        public ICommand DepositBoxDepositAllCommand { get; }

        public ICommand CollectionBoxOpenCommand { get; }
        public ICommand CollectionBoxCloseCommand { get; }
        public ICommand CollectionBoxCollectInvCommand { get; }
        public ICommand CollectionBoxCollectBankCommand { get; }
        public ICommand GetStackByIdCommand { get; }
        public ICommand GetStackByNameCommand { get; }
        public ICommand DoActionByIdCommand { get; }
        public ICommand DoActionByNameCommand { get; }
        public ICommand LoadItemsCommand { get; }
        public ICommand ClearItemsCommand { get; }

        private int _actionIndex, _offset;
        public int ActionIndex { get => _actionIndex; set => Set(ref _actionIndex, value); }
        public int Offset { get => _offset; set => Set(ref _offset, value); }

        // Items collection for DataGrid
        public ObservableCollection<ItemContainer> Items { get; } = new ObservableCollection<ItemContainer>();
        public int ItemCount => Items.Count;

        public BankViewModel()
        {
            RefreshStateCommand = new RelayCommand(_ =>
            {
                IsOpen = Bank.IsOpen;
                NoteModeEnabled = Bank.IsNoteModeEnabled();
            });

            OpenCommand = new RelayCommand(_ => { ActionResult = Bank.Open() ? "✔ Opened" : "✘ Failed"; IsOpen = Bank.IsOpen; });
            LoadLastPresetCommand = new RelayCommand(_ => ActionResult = Bank.LoadLastPreset() ? "✔ Loaded last preset" : "✘ Failed");
            EnterPinCommand = new RelayCommand(_ => ActionResult = TryEnterPin());
            
            CloseCommand = new RelayCommand(_ => { Bank.Close(); IsOpen = Bank.IsOpen; });
            
            DepositAllCommand = new RelayCommand(_ => ActionResult = Bank.DepositAll() ? "✔ Deposited" : "✘ Failed");
            
            DepositAllExceptIdsCommand = new RelayCommand(_ => {
                var ids = ParseIds(IdsInput);
                ActionResult = Bank.DepositAllExcept(ids) ? "✔ Deposited (except)" : "✘ Failed";});

            DepositAllExceptNamesCommand = new RelayCommand(_ => {
                var names = ParseNames(NamesInput);
                ActionResult = Bank.DepositAllExcept(names) ? "✔ Deposited (except)" : "✘ Failed";});

            DepositInventoryCommand = new RelayCommand(_ => ActionResult = Bank.DepositInventory() ? "✔ Deposited inventory" : "✘ Failed");
            DepositEquipmentCommand = new RelayCommand(_ => ActionResult = Bank.DepositEquipment() ? "✔ Deposited equipment" : "✘ Failed");
            DepositSummonCommand = new RelayCommand(_ => ActionResult = Bank.DepositSummon() ? "✔ Deposited summon" : "✘ Failed");
            DepositMoneyPouchCommand = new RelayCommand(_ => ActionResult = Bank.DepositMoneyPouch() ? "✔ Deposited money pouch" : "✘ Failed");

            OpenInventoryTabCommand = new RelayCommand(_ => ActionResult = Bank.OpenTab(Bank.BankTab.Inventory) ? "✔ Inventory tab" : "✘ Failed");
            OpenBoBTabCommand = new RelayCommand(_ => ActionResult = Bank.OpenTab(Bank.BankTab.BeastOfBurden) ? "✔ BoB tab" : "✘ Failed");
            OpenEquipmentTabCommand = new RelayCommand(_ => ActionResult = Bank.OpenTab(Bank.BankTab.Equipment) ? "✔ Equipment tab" : "✘ Failed");
            SetTransferTabCommand = new RelayCommand(_ => ActionResult = Bank.SetTransferTab(Bank.TransferTab.Transfer) ? "✔ Transfer tab" : "✘ Failed");
            SetPresetTabCommand = new RelayCommand(_ => ActionResult = Bank.SetTransferTab(Bank.TransferTab.Preset) ? "✔ Preset tab" : "✘ Failed");

            SetQuantity1Command = new RelayCommand(_ => ActionResult = Bank.SetQuantity(Bank.TransferQuantity.One) ? "✔ Qty 1" : "✘ Failed");
            SetQuantity5Command = new RelayCommand(_ => ActionResult = Bank.SetQuantity(Bank.TransferQuantity.Five) ? "✔ Qty 5" : "✘ Failed");
            SetQuantity10Command = new RelayCommand(_ => ActionResult = Bank.SetQuantity(Bank.TransferQuantity.Ten) ? "✔ Qty 10" : "✘ Failed");
            SetQuantityXCommand = new RelayCommand(_ => ActionResult = Bank.SetQuantity(Bank.TransferQuantity.X) ? "✔ Qty X" : "✘ Failed");
            SetQuantityAllCommand = new RelayCommand(_ => ActionResult = Bank.SetQuantity(Bank.TransferQuantity.All) ? "✔ Qty All" : "✘ Failed");
            SetXQuantityCommand = new RelayCommand(_ => ActionResult = TrySetXQuantity());
            ToggleNoteModeCommand = new RelayCommand(_ =>
            {
                var next = !NoteModeEnabled;
                ActionResult = Bank.SetNoteMode(next) ? $"✔ Note mode {(next ? "On" : "Off")}" : "✘ Failed";
                NoteModeEnabled = Bank.IsNoteModeEnabled();
            });

            SavePresetCommand = new RelayCommand(_ => ActionResult = TrySavePreset());
            LoadPresetCommand = new RelayCommand(_ => ActionResult = TryLoadPreset());
            SaveSummonPresetCommand = new RelayCommand(_ => ActionResult = Bank.SaveSummonPreset() ? "✔ Saved summon preset" : "✘ Failed");
            LoadSummonPresetCommand = new RelayCommand(_ => ActionResult = Bank.LoadSummonPreset() ? "✔ Loaded summon preset" : "✘ Failed");

            PresetSettingsOpenCommand = new RelayCommand(_ => ActionResult = Bank.PresetSettingsOpen() ? "✔ Preset settings open" : "✘ Failed");
            PresetSettingsReturnCommand = new RelayCommand(_ => ActionResult = Bank.PresetSettingsReturnToBank() ? "✔ Return to bank" : "✘ Failed");
            PresetSettingsSelectCommand = new RelayCommand(_ => ActionResult = TrySelectPresetSettings());

            DepositBoxOpenCommand = new RelayCommand(_ => ActionResult = Bank.DepositBoxOpen() ? "✔ Deposit box open" : "✘ Failed");
            DepositBoxCloseCommand = new RelayCommand(_ => ActionResult = Bank.DepositBoxClose() ? "✔ Deposit box close" : "✘ Failed");
            DepositBoxDepositInventoryCommand = new RelayCommand(_ => ActionResult = Bank.DepositBoxDepositInventory() ? "✔ Deposit inv" : "✘ Failed");
            DepositBoxDepositEquipmentCommand = new RelayCommand(_ => ActionResult = Bank.DepositBoxDepositEquipment() ? "✔ Deposit equip" : "✘ Failed");
            DepositBoxDepositMoneyCommand = new RelayCommand(_ => ActionResult = Bank.DepositBoxDepositMoneyPouch() ? "✔ Deposit money" : "✘ Failed");
            DepositBoxDepositAllCommand = new RelayCommand(_ => ActionResult = Bank.DepositBoxDepositAll() ? "✔ Deposit all" : "✘ Failed");

            CollectionBoxOpenCommand = new RelayCommand(_ => ActionResult = Bank.CollectionBoxOpen() ? "✔ Collection box open" : "✘ Failed");
            CollectionBoxCloseCommand = new RelayCommand(_ => ActionResult = Bank.CollectionBoxClose() ? "✔ Collection box close" : "✘ Failed");
            CollectionBoxCollectInvCommand = new RelayCommand(_ => ActionResult = Bank.CollectionBoxCollectToInventory() ? "✔ Collect inv" : "✘ Failed");
            CollectionBoxCollectBankCommand = new RelayCommand(_ => ActionResult = Bank.CollectionBoxCollectToBank() ? "✔ Collect bank" : "✘ Failed");

            GetStackByIdCommand = new RelayCommand(_ => {
                if (int.TryParse(IdInput, out var id)) StackResult = Bank.GetStack(id).ToString();});

            GetStackByNameCommand = new RelayCommand(_ => StackResult = Bank.GetStack(NameInput).ToString());
            
            DoActionByIdCommand = new RelayCommand(_ => {
                if (int.TryParse(IdInput, out var id)) ActionResult = Bank.DoActionById(id, ActionIndex, Offset) ? "✔ OK" : "✘ Failed";});

            DoActionByNameCommand = new RelayCommand(_ => ActionResult = Bank.DoActionByName(NameInput, ActionIndex, Offset) ? "✔ OK" : "✘ Failed");

            LoadItemsCommand = new RelayCommand(_ => LoadItems());
            ClearItemsCommand = new RelayCommand(_ => ClearItems());

            UpdateQuickFilter();
        }

        private static int[] ParseIds(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();
            var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new System.Collections.Generic.List<int>(parts.Length);
            foreach (var p in parts) if (int.TryParse(p.Trim(), out var id)) list.Add(id);
            return list.ToArray();
        }

        private static string[] ParseNames(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!Equals(field, value)) { field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); return true; }
            return false;
        }

        private void UpdateQuickFilter()
        {
            QuickStack = 0;
            var q = QuickFilterText;
            if (string.IsNullOrWhiteSpace(q)) { StackResult = string.Empty; return; }
            if (int.TryParse(q, out var id))
            {
                IdInput = q;
                var s = Bank.GetStack(id);
                QuickStack = (int)s;
                StackResult = s.ToString();
            }
            else
            {
                NameInput = q;
                var s = Bank.GetStack(q);
                QuickStack = (int)s;
                StackResult = s.ToString();
            }
        }

		private void LoadItems()
		{
			Items.Clear();
			var items = Bank.GetItemsDetailed(includeCoordinates: false);
			foreach (var item in items)
			{
				Items.Add(item);
			}
			OnPropertyChanged(nameof(ItemCount));
		}

		private void ClearItems()
		{
			Items.Clear();
			OnPropertyChanged(nameof(ItemCount));
		}

		private void OnPropertyChanged(string v)
		{
			if (string.IsNullOrEmpty(v)) return;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
		}

        private string TryEnterPin()
        {
            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 4) return "✘ Enter 4 digits";
            if (!int.TryParse(PinInput[0].ToString(), out var d1)) return "✘ Invalid PIN";
            if (!int.TryParse(PinInput[1].ToString(), out var d2)) return "✘ Invalid PIN";
            if (!int.TryParse(PinInput[2].ToString(), out var d3)) return "✘ Invalid PIN";
            if (!int.TryParse(PinInput[3].ToString(), out var d4)) return "✘ Invalid PIN";
            return Bank.EnterPin(d1, d2, d3, d4) ? "✔ PIN sent" : "✘ Failed";
        }

        private string TrySetXQuantity()
        {
            if (!int.TryParse(XQuantityInput, out var qty) || qty <= 0) return "✘ Invalid X";
            return Bank.SetXQuantity(qty) ? "✔ Set X" : "✘ Failed";
        }

        private string TrySavePreset()
        {
            if (!int.TryParse(PresetNumberInput, out var number)) return "✘ Invalid preset";
            return Bank.SavePreset(number) ? "✔ Preset saved" : "✘ Failed";
        }

        private string TryLoadPreset()
        {
            if (!int.TryParse(PresetNumberInput, out var number)) return "✘ Invalid preset";
            return Bank.LoadPreset(number) ? "✔ Preset loaded" : "✘ Failed";
        }

        private string TrySelectPresetSettings()
        {
            if (!int.TryParse(PresetSettingsNumberInput, out var preset)) return "✘ Invalid preset";
            return Bank.PresetSettingsSelectPreset(preset) ? "✔ Preset selected" : "✘ Failed";
        }
	}
}
