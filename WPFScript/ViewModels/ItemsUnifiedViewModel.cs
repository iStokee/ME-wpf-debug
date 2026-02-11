using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MESharp.ViewModels
{
	public class ItemsUnifiedViewModel : INotifyPropertyChanged, IActivatableViewModel
	{
		// ─── State ───────────────────────────────────────────────────────────
		private ContainerType _selectedContainer = ContainerType.Inventory;
		private bool _includeCoordinates;
		private bool _isSidePanelCollapsed;

		public bool IsSidePanelCollapsed
		{
			get => _isSidePanelCollapsed;
			set => SetProperty(ref _isSidePanelCollapsed, value);
		}

		public ContainerType SelectedContainer
		{
			get => _selectedContainer;
			set
			{
				if (SetProperty(ref _selectedContainer, value))
				{
					UpdateContainerVisibility();
				}
			}
		}

		public bool IncludeCoordinates
		{
			get => _includeCoordinates;
			set => SetProperty(ref _includeCoordinates, value);
		}

		public ContainerType[] AvailableContainers => (ContainerType[])Enum.GetValues(typeof(ContainerType));

		// ─── Collections ─────────────────────────────────────────────────────
		public ObservableCollection<ItemContainer> Items { get; } = new ObservableCollection<ItemContainer>();

		private ItemContainer _selectedItem;
		public ItemContainer SelectedItem
		{
			get => _selectedItem;
			set => SetProperty(ref _selectedItem, value);
		}

		private ItemContainer _targetItem;
		public ItemContainer TargetItem
		{
			get => _targetItem;
			set => SetProperty(ref _targetItem, value);
		}

		// ─── Commands ────────────────────────────────────────────────────────
		public ICommand LoadItemsCommand { get; }
		public ICommand ClearCommand { get; }
		public ICommand ToggleSidePanelCommand { get; }

			// Container-specific commands
			public ICommand InventoryRefreshCommand { get; }
			public ICommand InventoryDoActionByIdCommand { get; }
			public ICommand InventoryDoActionByNameCommand { get; }
			public ICommand InventoryDoActionBySlotCommand { get; }
			public ICommand InventoryRubByIdCommand { get; }
			public ICommand InventoryRubByNameCommand { get; }
			public ICommand InventoryResolveRootCommand { get; }
			public ICommand BankOpenCommand { get; }
		public ICommand BankLoadLastPresetCommand { get; }
		public ICommand BankEnterPinCommand { get; }
		public ICommand BankDepositAllCommand { get; }
		public ICommand BankDepositExceptIdsCommand { get; }
		public ICommand BankDepositExceptNamesCommand { get; }
		public ICommand BankCloseCommand { get; }
		public ICommand BankWithdrawSelectedCommand { get; }
		public ICommand BankWithdrawByIdCommand { get; }
		public ICommand BankWithdrawByNameCommand { get; }
		public ICommand BankInvDepositSelectedCommand { get; }
		public ICommand BankInvDepositByIdCommand { get; }
		public ICommand BankInvDepositByNameCommand { get; }
		public ICommand BankDepositInventoryCommand { get; }
		public ICommand BankDepositEquipmentCommand { get; }
		public ICommand BankDepositSummonCommand { get; }
		public ICommand BankDepositMoneyPouchCommand { get; }
		public ICommand BankOpenInventoryTabCommand { get; }
		public ICommand BankOpenBoBTabCommand { get; }
		public ICommand BankOpenEquipmentTabCommand { get; }
		public ICommand BankSetTransferTabCommand { get; }
		public ICommand BankSetPresetTabCommand { get; }
		public ICommand BankSetQuantity1Command { get; }
		public ICommand BankSetQuantity5Command { get; }
		public ICommand BankSetQuantity10Command { get; }
		public ICommand BankSetQuantityXCommand { get; }
		public ICommand BankSetQuantityAllCommand { get; }
		public ICommand BankSetXQuantityCommand { get; }
		public ICommand BankToggleNoteModeCommand { get; }
		public ICommand BankSavePresetCommand { get; }
		public ICommand BankLoadPresetCommand { get; }
		public ICommand BankSaveSummonPresetCommand { get; }
		public ICommand BankLoadSummonPresetCommand { get; }
		public ICommand BankPresetSettingsOpenCommand { get; }
		public ICommand BankPresetSettingsReturnCommand { get; }
		public ICommand BankPresetSettingsSelectCommand { get; }
		public ICommand BankDepositBoxOpenCommand { get; }
		public ICommand BankDepositBoxCloseCommand { get; }
		public ICommand BankDepositBoxDepositInventoryCommand { get; }
		public ICommand BankDepositBoxDepositEquipmentCommand { get; }
		public ICommand BankDepositBoxDepositMoneyCommand { get; }
		public ICommand BankDepositBoxDepositAllCommand { get; }
		public ICommand BankCollectionBoxOpenCommand { get; }
		public ICommand BankCollectionBoxCloseCommand { get; }
		public ICommand BankCollectionBoxCollectInvCommand { get; }
		public ICommand BankCollectionBoxCollectBankCommand { get; }
		public ICommand BankGetStackByIdCommand { get; }
		public ICommand BankGetStackByNameCommand { get; }
		public ICommand BankDoActionByIdCommand { get; }
		public ICommand BankDoActionByNameCommand { get; }
		public ICommand EquipmentRefreshCommand { get; }
		public ICommand EquipmentOpenCommand { get; }
		public ICommand LootRefreshCommand { get; }
		public ICommand MaterialCacheRefreshCommand { get; }
		public ICommand TradeWindowRefreshCommand { get; }

		// Familiar commands
		public ICommand FamiliarRefreshCommand { get; }
		public ICommand FamiliarCastSpecialCommand { get; }

		// Item action commands (for selected item)
		public ICommand ItemEatCommand { get; }
		public ICommand ItemDropCommand { get; }
		public ICommand ItemUseCommand { get; }
		public ICommand ItemEquipCommand { get; }
		public ICommand ItemNoteCommand { get; }
		public ICommand ItemUnequipCommand { get; }
		public ICommand ItemUseOnItemCommand { get; }

		// ─── Summary Stats ───────────────────────────────────────────────────
		private int _itemCount;
		private string _statusMessage;

		public int ItemCount
		{
			get => _itemCount;
			set => SetProperty(ref _itemCount, value);
		}

		public string StatusMessage
		{
			get => _statusMessage;
			set => SetProperty(ref _statusMessage, value);
		}

		// ─── Container-specific visibility ──────────────────────────────────
		private bool _isInventorySelected;
		private bool _isBankSelected;
		private bool _isEquipmentSelected;
		private bool _isLootSelected;
		private bool _isMaterialCacheSelected;
		private bool _isTradeWindowSelected;
		private bool _isFamiliarSelected;

		public bool IsInventorySelected
		{
			get => _isInventorySelected;
			set => SetProperty(ref _isInventorySelected, value);
		}

		public bool IsBankSelected
		{
			get => _isBankSelected;
			set => SetProperty(ref _isBankSelected, value);
		}

		public bool IsEquipmentSelected
		{
			get => _isEquipmentSelected;
			set => SetProperty(ref _isEquipmentSelected, value);
		}

		public bool IsLootSelected
		{
			get => _isLootSelected;
			set => SetProperty(ref _isLootSelected, value);
		}

		public bool IsMaterialCacheSelected
		{
			get => _isMaterialCacheSelected;
			set => SetProperty(ref _isMaterialCacheSelected, value);
		}

		public bool IsTradeWindowSelected
		{
			get => _isTradeWindowSelected;
			set => SetProperty(ref _isTradeWindowSelected, value);
		}

		public bool IsFamiliarSelected
		{
			get => _isFamiliarSelected;
			set => SetProperty(ref _isFamiliarSelected, value);
		}

		// ─── Familiar-specific properties ───────────────────────────────────
		private bool _hasFamiliar;
		private string _familiarName = "";
		private int _familiarTimeRemaining;
		private bool _familiarCanRenew;
		private int _familiarSpellPoints;
		private int _familiarHealth;

		public bool HasFamiliar
		{
			get => _hasFamiliar;
			set => SetProperty(ref _hasFamiliar, value);
		}

		public string FamiliarName
		{
			get => _familiarName;
			set => SetProperty(ref _familiarName, value);
		}

		public int FamiliarTimeRemaining
		{
			get => _familiarTimeRemaining;
			set => SetProperty(ref _familiarTimeRemaining, value);
		}

		public bool FamiliarCanRenew
		{
			get => _familiarCanRenew;
			set => SetProperty(ref _familiarCanRenew, value);
		}

		public int FamiliarSpellPoints
		{
			get => _familiarSpellPoints;
			set => SetProperty(ref _familiarSpellPoints, value);
		}

		public int FamiliarHealth
		{
			get => _familiarHealth;
			set => SetProperty(ref _familiarHealth, value);
		}

		// ─── Bank-specific properties ───────────────────────────────────────
		private string _bankKeepIds = string.Empty;
		public string BankKeepIds
		{
			get => _bankKeepIds;
			set => SetProperty(ref _bankKeepIds, value);
		}

		private string _bankMenuText = "Withdraw-1";
		public string BankMenuText
		{
			get => _bankMenuText;
			set => SetProperty(ref _bankMenuText, value);
		}

		private string _bankActionId = string.Empty;
		public string BankActionId
		{
			get => _bankActionId;
			set => SetProperty(ref _bankActionId, value);
		}

		private string _bankActionName = string.Empty;
		public string BankActionName
		{
			get => _bankActionName;
			set => SetProperty(ref _bankActionName, value);
		}

		private string _bankInvMenuText = "Deposit-1";
		public string BankInvMenuText
		{
			get => _bankInvMenuText;
			set => SetProperty(ref _bankInvMenuText, value);
		}

		private string _bankInvActionId = string.Empty;
		public string BankInvActionId
		{
			get => _bankInvActionId;
			set => SetProperty(ref _bankInvActionId, value);
		}

		private string _bankInvActionName = string.Empty;
		public string BankInvActionName
		{
			get => _bankInvActionName;
			set => SetProperty(ref _bankInvActionName, value);
		}

		private string _bankKeepNames = string.Empty;
		public string BankKeepNames
		{
			get => _bankKeepNames;
			set => SetProperty(ref _bankKeepNames, value);
		}

		private string _pinInput = string.Empty;
		public string PinInput
		{
			get => _pinInput;
			set => SetProperty(ref _pinInput, value);
		}

		private string _presetNumberInput = string.Empty;
		public string PresetNumberInput
		{
			get => _presetNumberInput;
			set => SetProperty(ref _presetNumberInput, value);
		}

		private string _presetSettingsNumberInput = string.Empty;
		public string PresetSettingsNumberInput
		{
			get => _presetSettingsNumberInput;
			set => SetProperty(ref _presetSettingsNumberInput, value);
		}

		private string _xQuantityInput = string.Empty;
		public string XQuantityInput
		{
			get => _xQuantityInput;
			set => SetProperty(ref _xQuantityInput, value);
		}

		private string _idsInput = string.Empty;
		public string IdsInput
		{
			get => _idsInput;
			set => SetProperty(ref _idsInput, value);
		}

		private string _namesInput = string.Empty;
		public string NamesInput
		{
			get => _namesInput;
			set => SetProperty(ref _namesInput, value);
		}

		private string _idInput = string.Empty;
		public string IdInput
		{
			get => _idInput;
			set => SetProperty(ref _idInput, value);
		}

		private string _nameInput = string.Empty;
		public string NameInput
		{
			get => _nameInput;
			set => SetProperty(ref _nameInput, value);
		}

		private int _actionIndex;
		public int ActionIndex
		{
			get => _actionIndex;
			set => SetProperty(ref _actionIndex, value);
		}

		private int _offset;
		public int Offset
		{
			get => _offset;
			set => SetProperty(ref _offset, value);
		}

		private string _stackResult = string.Empty;
		public string StackResult
		{
			get => _stackResult;
			set => SetProperty(ref _stackResult, value);
		}

		// ─── Status Properties ──────────────────────────────────────────────
		private bool _inventoryIsOpen;
		private bool _inventoryIsFull;
		private bool _inventoryIsEmpty;
		private bool _inventoryItemSelected;
		private int _inventoryFreeSlots;

		public bool InventoryIsOpen
		{
			get => _inventoryIsOpen;
			set => SetProperty(ref _inventoryIsOpen, value);
		}

		public bool InventoryIsFull
		{
			get => _inventoryIsFull;
			set => SetProperty(ref _inventoryIsFull, value);
		}

		public bool InventoryIsEmpty
		{
			get => _inventoryIsEmpty;
			set => SetProperty(ref _inventoryIsEmpty, value);
		}

		public bool InventoryItemSelected
		{
			get => _inventoryItemSelected;
			set => SetProperty(ref _inventoryItemSelected, value);
		}

			public int InventoryFreeSlots
			{
				get => _inventoryFreeSlots;
				set => SetProperty(ref _inventoryFreeSlots, value);
			}

			// ─── Inventory Advanced Actions (moved from API Utilities) ───────────
			private int _inventoryActionItemId = 15707;
			public int InventoryActionItemId
			{
				get => _inventoryActionItemId;
				set => SetProperty(ref _inventoryActionItemId, value);
			}

			private string _inventoryActionItemName = "Ring of kinship";
			public string InventoryActionItemName
			{
				get => _inventoryActionItemName;
				set => SetProperty(ref _inventoryActionItemName, value);
			}

			private int _inventoryActionMenuIndex = 2;
			public int InventoryActionMenuIndex
			{
				get => _inventoryActionMenuIndex;
				set => SetProperty(ref _inventoryActionMenuIndex, value);
			}

			public IReadOnlyList<ActionOffsetOption> InventoryActionOffsetOptions { get; }

			private ActionOffsetOption _inventoryActionSelectedOffset;
			public ActionOffsetOption InventoryActionSelectedOffset
			{
				get => _inventoryActionSelectedOffset;
				set => SetProperty(ref _inventoryActionSelectedOffset, value);
			}
	
			private bool _bankIsOpen;
			public bool BankIsOpen
			{
			get => _bankIsOpen;
			set => SetProperty(ref _bankIsOpen, value);
		}

		private bool _bankNoteModeEnabled;
		public bool BankNoteModeEnabled
		{
			get => _bankNoteModeEnabled;
			set => SetProperty(ref _bankNoteModeEnabled, value);
		}

		private Bank.TransferQuantity _bankQuantitySelected;
		public Bank.TransferQuantity BankQuantitySelected
		{
			get => _bankQuantitySelected;
			set => SetProperty(ref _bankQuantitySelected, value);
		}

		private int _bankXQuantity;
		public int BankXQuantity
		{
			get => _bankXQuantity;
			set => SetProperty(ref _bankXQuantity, value);
		}

		private bool _equipmentIsOpen;
		public bool EquipmentIsOpen
		{
			get => _equipmentIsOpen;
			set => SetProperty(ref _equipmentIsOpen, value);
		}

			public ItemsUnifiedViewModel()
			{
				InventoryActionOffsetOptions = ActionOffsets.All
					.Where(o => o.Category == ActionOffsets.OffsetCategory.Interface)
					.OrderBy(o => o.Label)
					.Select(o => new ActionOffsetOption(o))
					.ToList();
				InventoryActionSelectedOffset = InventoryActionOffsetOptions.FirstOrDefault(o => o.Value == Objects.Offsets.GeneralInterfaceRoute)
					?? InventoryActionOffsetOptions.FirstOrDefault()
					?? new ActionOffsetOption(ActionOffsets.All.First());

				LoadItemsCommand = new RelayCommand(_ => LoadItems());
				ClearCommand = new RelayCommand(_ =>
				{
					Items.Clear();
				SelectedItem = null;
				ItemCount = 0;
				StatusMessage = "Cleared.";
			});
			ToggleSidePanelCommand = new RelayCommand(_ => IsSidePanelCollapsed = !IsSidePanelCollapsed);

				// Container-specific commands
				InventoryRefreshCommand = new RelayCommand(_ => { LoadItems(); RefreshInventoryStatus(); });
				InventoryDoActionByIdCommand = new RelayCommand(_ => InventoryDoActionById());
				InventoryDoActionByNameCommand = new RelayCommand(_ => InventoryDoActionByName());
				InventoryDoActionBySlotCommand = new RelayCommand(_ => InventoryDoActionBySlotFallback());
				InventoryRubByIdCommand = new RelayCommand(_ => InventoryRubById());
				InventoryRubByNameCommand = new RelayCommand(_ => InventoryRubByName());
				InventoryResolveRootCommand = new RelayCommand(_ => InventoryResolveRoot());
				BankOpenCommand = new RelayCommand(_ => BankOpen());
				BankLoadLastPresetCommand = new RelayCommand(_ => BankLoadLastPreset());
				BankEnterPinCommand = new RelayCommand(_ => BankEnterPin());
			BankDepositAllCommand = new RelayCommand(_ => BankDepositAll());
			BankDepositExceptIdsCommand = new RelayCommand(_ => BankDepositExceptIds());
			BankDepositExceptNamesCommand = new RelayCommand(_ => BankDepositExceptNames());
			BankCloseCommand = new RelayCommand(_ => BankClose());
			BankWithdrawSelectedCommand = new RelayCommand(_ => BankWithdrawSelected());
			BankWithdrawByIdCommand = new RelayCommand(_ => BankWithdrawById());
			BankWithdrawByNameCommand = new RelayCommand(_ => BankWithdrawByName());
			BankInvDepositSelectedCommand = new RelayCommand(_ => BankInvDepositSelected());
			BankInvDepositByIdCommand = new RelayCommand(_ => BankInvDepositById());
			BankInvDepositByNameCommand = new RelayCommand(_ => BankInvDepositByName());
			BankDepositInventoryCommand = new RelayCommand(_ => BankDepositInventory());
			BankDepositEquipmentCommand = new RelayCommand(_ => BankDepositEquipment());
			BankDepositSummonCommand = new RelayCommand(_ => BankDepositSummon());
			BankDepositMoneyPouchCommand = new RelayCommand(_ => BankDepositMoneyPouch());
			BankOpenInventoryTabCommand = new RelayCommand(_ => BankOpenTab(Bank.BankTab.Inventory));
			BankOpenBoBTabCommand = new RelayCommand(_ => BankOpenTab(Bank.BankTab.BeastOfBurden));
			BankOpenEquipmentTabCommand = new RelayCommand(_ => BankOpenTab(Bank.BankTab.Equipment));
			BankSetTransferTabCommand = new RelayCommand(_ => BankSetTransferTab(Bank.TransferTab.Transfer));
			BankSetPresetTabCommand = new RelayCommand(_ => BankSetTransferTab(Bank.TransferTab.Preset));
			BankSetQuantity1Command = new RelayCommand(_ => BankSetQuantity(Bank.TransferQuantity.One));
			BankSetQuantity5Command = new RelayCommand(_ => BankSetQuantity(Bank.TransferQuantity.Five));
			BankSetQuantity10Command = new RelayCommand(_ => BankSetQuantity(Bank.TransferQuantity.Ten));
			BankSetQuantityXCommand = new RelayCommand(_ => BankSetQuantity(Bank.TransferQuantity.X));
			BankSetQuantityAllCommand = new RelayCommand(_ => BankSetQuantity(Bank.TransferQuantity.All));
			BankSetXQuantityCommand = new RelayCommand(_ => BankSetXQuantity());
			BankToggleNoteModeCommand = new RelayCommand(_ => BankToggleNoteMode());
			BankSavePresetCommand = new RelayCommand(_ => BankSavePreset());
			BankLoadPresetCommand = new RelayCommand(_ => BankLoadPreset());
			BankSaveSummonPresetCommand = new RelayCommand(_ => BankSaveSummonPreset());
			BankLoadSummonPresetCommand = new RelayCommand(_ => BankLoadSummonPreset());
			BankPresetSettingsOpenCommand = new RelayCommand(_ => BankPresetSettingsOpen());
			BankPresetSettingsReturnCommand = new RelayCommand(_ => BankPresetSettingsReturn());
			BankPresetSettingsSelectCommand = new RelayCommand(_ => BankPresetSettingsSelect());
			BankDepositBoxOpenCommand = new RelayCommand(_ => BankDepositBoxOpen());
			BankDepositBoxCloseCommand = new RelayCommand(_ => BankDepositBoxClose());
			BankDepositBoxDepositInventoryCommand = new RelayCommand(_ => BankDepositBoxDepositInventory());
			BankDepositBoxDepositEquipmentCommand = new RelayCommand(_ => BankDepositBoxDepositEquipment());
			BankDepositBoxDepositMoneyCommand = new RelayCommand(_ => BankDepositBoxDepositMoney());
			BankDepositBoxDepositAllCommand = new RelayCommand(_ => BankDepositBoxDepositAll());
			BankCollectionBoxOpenCommand = new RelayCommand(_ => BankCollectionBoxOpen());
			BankCollectionBoxCloseCommand = new RelayCommand(_ => BankCollectionBoxClose());
			BankCollectionBoxCollectInvCommand = new RelayCommand(_ => BankCollectionBoxCollectInv());
			BankCollectionBoxCollectBankCommand = new RelayCommand(_ => BankCollectionBoxCollectBank());
			BankGetStackByIdCommand = new RelayCommand(_ => BankGetStackById());
			BankGetStackByNameCommand = new RelayCommand(_ => BankGetStackByName());
			BankDoActionByIdCommand = new RelayCommand(_ => BankDoActionById());
			BankDoActionByNameCommand = new RelayCommand(_ => BankDoActionByName());
			EquipmentRefreshCommand = new RelayCommand(_ => { LoadItems(); RefreshEquipmentStatus(); });
			EquipmentOpenCommand = new RelayCommand(_ => EquipmentOpen());
			LootRefreshCommand = new RelayCommand(_ => LoadItems());
			MaterialCacheRefreshCommand = new RelayCommand(_ => LoadItems());
			TradeWindowRefreshCommand = new RelayCommand(_ => LoadItems());

			// Familiar commands
			FamiliarRefreshCommand = new RelayCommand(_ => RefreshFamiliarStatus());
			FamiliarCastSpecialCommand = new RelayCommand(_ => FamiliarCastSpecial());

			// Item action commands
			ItemEatCommand = new RelayCommand(_ => ItemEat(), _ => SelectedItem != null && IsInventorySelected);
			ItemDropCommand = new RelayCommand(_ => ItemDrop(), _ => SelectedItem != null && IsInventorySelected);
			ItemUseCommand = new RelayCommand(_ => ItemUse(), _ => SelectedItem != null && IsInventorySelected);
			ItemEquipCommand = new RelayCommand(_ => ItemEquip(), _ => SelectedItem != null && IsInventorySelected);
			ItemNoteCommand = new RelayCommand(_ => ItemNote(), _ => SelectedItem != null && IsInventorySelected);
			ItemUnequipCommand = new RelayCommand(_ => ItemUnequip(), _ => SelectedItem != null && IsEquipmentSelected);
			ItemUseOnItemCommand = new RelayCommand(_ => ItemUseOnItem(), _ => SelectedItem != null && TargetItem != null && IsInventorySelected);

			StatusMessage = "Select a container and click Load Items.";
			UpdateContainerVisibility();
		}

		private void UpdateContainerVisibility()
		{
			IsInventorySelected = SelectedContainer == ContainerType.Inventory;
			IsBankSelected = SelectedContainer == ContainerType.Bank || SelectedContainer == ContainerType.BankInventory;
			IsEquipmentSelected = SelectedContainer == ContainerType.Equipment;
			IsLootSelected = SelectedContainer == ContainerType.Loot;
			IsMaterialCacheSelected = SelectedContainer == ContainerType.MaterialCache;
			IsTradeWindowSelected = SelectedContainer == ContainerType.TradeWindow;
			IsFamiliarSelected = SelectedContainer == ContainerType.Familiar;

			// Refresh status when switching containers
			if (IsInventorySelected) RefreshInventoryStatus();
			else if (IsBankSelected) RefreshBankStatus();
			else if (IsEquipmentSelected) RefreshEquipmentStatus();
			else if (IsFamiliarSelected) RefreshFamiliarStatus();
		}

		private void LoadItems()
		{
			try
			{
				Items.Clear();
				SelectedItem = null;

				var items = ItemContainers.Read(SelectedContainer, IncludeCoordinates);

				foreach (var item in items)
				{
					Items.Add(item);
				}

				var inGameSelection = Items.FirstOrDefault(i => i.IsSelected);
				if (inGameSelection != null)
				{
					SelectedItem = inGameSelection;
				}

				if (IsInventorySelected)
				{
					InventoryItemSelected = Inventory.IsItemSelected;
				}
				if (IsBankSelected)
				{
					RefreshBankStatus();
				}

				ItemCount = Items.Count;
				StatusMessage = $"Loaded {ItemCount} items from {SelectedContainer}.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositAll()
		{
			try
			{
				Bank.DepositAll();
				StatusMessage = "Deposited all items to bank.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankOpen()
		{
			try
			{
				var ok = Bank.Open();
				StatusMessage = ok ? "Opened bank." : "Failed to open bank.";
				RefreshBankStatus();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankLoadLastPreset()
		{
			try
			{
				var ok = Bank.LoadLastPreset();
				StatusMessage = ok ? "Loaded last preset." : "Failed to load last preset.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankEnterPin()
		{
			try
			{
				StatusMessage = TryEnterPin();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositExceptIds()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(BankKeepIds))
				{
					StatusMessage = "Please enter item IDs to keep.";
					return;
				}

				var ids = BankKeepIds.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
				var idArray = new int[ids.Length];
				for (int i = 0; i < ids.Length; i++)
				{
					if (!int.TryParse(ids[i], out idArray[i]))
					{
						StatusMessage = $"Invalid ID: {ids[i]}";
						return;
					}
				}

				Bank.DepositAllExcept(idArray);
				StatusMessage = $"Deposited all items except IDs: {BankKeepIds}";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositExceptNames()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(BankKeepNames))
				{
					StatusMessage = "Please enter item names to keep.";
					return;
				}

				var names = ParseNames(BankKeepNames);
				if (names.Length == 0)
				{
					StatusMessage = "No valid names to keep.";
					return;
				}

				Bank.DepositAllExcept(names);
				StatusMessage = $"Deposited all items except names: {BankKeepNames}";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositInventory()
		{
			try
			{
				var ok = Bank.DepositInventory();
				StatusMessage = ok ? "Deposited inventory." : "Deposit inventory failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositEquipment()
		{
			try
			{
				var ok = Bank.DepositEquipment();
				StatusMessage = ok ? "Deposited equipment." : "Deposit equipment failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositSummon()
		{
			try
			{
				var ok = Bank.DepositSummon();
				StatusMessage = ok ? "Deposited summon." : "Deposit summon failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositMoneyPouch()
		{
			try
			{
				var ok = Bank.DepositMoneyPouch();
				StatusMessage = ok ? "Deposited money pouch." : "Deposit money pouch failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankOpenTab(Bank.BankTab tab)
		{
			try
			{
				var ok = Bank.OpenTab(tab);
				StatusMessage = ok ? $"Opened {tab} tab." : $"Failed to open {tab} tab.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankSetTransferTab(Bank.TransferTab tab)
		{
			try
			{
				var ok = Bank.SetTransferTab(tab);
				StatusMessage = ok ? $"Set transfer tab to {tab}." : $"Failed to set {tab} tab.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankSetQuantity(Bank.TransferQuantity quantity)
		{
			try
			{
				var ok = Bank.SetQuantity(quantity);
				BankQuantitySelected = Bank.GetQuantitySelected();
				StatusMessage = ok ? $"Set quantity {quantity}." : $"Failed to set quantity {quantity}.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankSetXQuantity()
		{
			try
			{
				StatusMessage = TrySetXQuantity();
				BankXQuantity = Bank.GetXQuantity();
				BankQuantitySelected = Bank.GetQuantitySelected();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankToggleNoteMode()
		{
			try
			{
				var next = !BankNoteModeEnabled;
				var ok = Bank.SetNoteMode(next);
				BankNoteModeEnabled = Bank.IsNoteModeEnabled();
				StatusMessage = ok ? $"Note mode {(BankNoteModeEnabled ? "On" : "Off")}." : "Failed to toggle note mode.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankSavePreset()
		{
			try
			{
				StatusMessage = TrySavePreset();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankLoadPreset()
		{
			try
			{
				StatusMessage = TryLoadPreset();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankSaveSummonPreset()
		{
			try
			{
				var ok = Bank.SaveSummonPreset();
				StatusMessage = ok ? "Saved summon preset." : "Failed to save summon preset.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankLoadSummonPreset()
		{
			try
			{
				var ok = Bank.LoadSummonPreset();
				StatusMessage = ok ? "Loaded summon preset." : "Failed to load summon preset.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankPresetSettingsOpen()
		{
			try
			{
				var ok = Bank.PresetSettingsOpen();
				StatusMessage = ok ? "Opened preset settings." : "Failed to open preset settings.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankPresetSettingsReturn()
		{
			try
			{
				var ok = Bank.PresetSettingsReturnToBank();
				StatusMessage = ok ? "Returned to bank." : "Failed to return to bank.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankPresetSettingsSelect()
		{
			try
			{
				StatusMessage = TrySelectPresetSettings();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositBoxOpen()
		{
			try
			{
				var ok = Bank.DepositBoxOpen();
				StatusMessage = ok ? "Opened deposit box." : "Failed to open deposit box.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositBoxClose()
		{
			try
			{
				var ok = Bank.DepositBoxClose();
				StatusMessage = ok ? "Closed deposit box." : "Failed to close deposit box.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositBoxDepositInventory()
		{
			try
			{
				var ok = Bank.DepositBoxDepositInventory();
				StatusMessage = ok ? "Deposited inventory to box." : "Deposit inventory failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositBoxDepositEquipment()
		{
			try
			{
				var ok = Bank.DepositBoxDepositEquipment();
				StatusMessage = ok ? "Deposited equipment to box." : "Deposit equipment failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositBoxDepositMoney()
		{
			try
			{
				var ok = Bank.DepositBoxDepositMoneyPouch();
				StatusMessage = ok ? "Deposited money pouch to box." : "Deposit money failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDepositBoxDepositAll()
		{
			try
			{
				var ok = Bank.DepositBoxDepositAll();
				StatusMessage = ok ? "Deposited all to box." : "Deposit all failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankCollectionBoxOpen()
		{
			try
			{
				var ok = Bank.CollectionBoxOpen();
				StatusMessage = ok ? "Opened collection box." : "Failed to open collection box.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankCollectionBoxClose()
		{
			try
			{
				var ok = Bank.CollectionBoxClose();
				StatusMessage = ok ? "Closed collection box." : "Failed to close collection box.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankCollectionBoxCollectInv()
		{
			try
			{
				var ok = Bank.CollectionBoxCollectToInventory();
				StatusMessage = ok ? "Collected to inventory." : "Collect to inventory failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankCollectionBoxCollectBank()
		{
			try
			{
				var ok = Bank.CollectionBoxCollectToBank();
				StatusMessage = ok ? "Collected to bank." : "Collect to bank failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankGetStackById()
		{
			if (!int.TryParse(IdInput, out var id))
			{
				StatusMessage = "Enter a valid item ID.";
				return;
			}
			try
			{
				var s = Bank.GetStack(id);
				StackResult = s.ToString();
				StatusMessage = $"Stack for {id}: {StackResult}";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankGetStackByName()
		{
			if (string.IsNullOrWhiteSpace(NameInput))
			{
				StatusMessage = "Enter a valid item name.";
				return;
			}
			try
			{
				var s = Bank.GetStack(NameInput);
				StackResult = s.ToString();
				StatusMessage = $"Stack for {NameInput}: {StackResult}";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDoActionById()
		{
			if (!int.TryParse(IdInput, out var id))
			{
				StatusMessage = "Enter a valid item ID.";
				return;
			}
			try
			{
				var ok = Bank.DoActionById(id, ActionIndex, Offset);
				StatusMessage = ok ? "Bank action sent." : "Bank action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankDoActionByName()
		{
			if (string.IsNullOrWhiteSpace(NameInput))
			{
				StatusMessage = "Enter a valid item name.";
				return;
			}
			try
			{
				var ok = Bank.DoActionByName(NameInput, ActionIndex, Offset);
				StatusMessage = ok ? "Bank action sent." : "Bank action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}
		private void BankClose()
		{
			try
			{
				Bank.Close();
				StatusMessage = "Bank closed.";
				RefreshBankStatus();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankWithdrawSelected()
		{
			if (SelectedItem == null)
			{
				StatusMessage = "Select a bank item first.";
				return;
			}
			try
			{
				var ok = Bank.WithdrawById(SelectedItem.Id, BankMenuText);
				StatusMessage = ok ? $"Withdraw action sent for {SelectedItem.Name}." : "Withdraw action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankWithdrawById()
		{
			if (!int.TryParse(BankActionId, out var id))
			{
				StatusMessage = "Enter a valid item ID.";
				return;
			}
			try
			{
				var ok = Bank.WithdrawById(id, BankMenuText);
				StatusMessage = ok ? $"Withdraw action sent for ID {id}." : "Withdraw action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankWithdrawByName()
		{
			if (string.IsNullOrWhiteSpace(BankActionName))
			{
				StatusMessage = "Enter a valid item name.";
				return;
			}
			try
			{
				var ok = Bank.WithdrawByName(BankActionName, BankMenuText);
				StatusMessage = ok ? $"Withdraw action sent for '{BankActionName}'." : "Withdraw action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankInvDepositSelected()
		{
			if (SelectedItem == null)
			{
				StatusMessage = "Select a bank inventory item first.";
				return;
			}
			try
			{
				var ok = Bank.DepositFromInventoryById(SelectedItem.Id, BankInvMenuText);
				StatusMessage = ok ? $"Deposit action sent for {SelectedItem.Name}." : "Deposit action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankInvDepositById()
		{
			if (!int.TryParse(BankInvActionId, out var id))
			{
				StatusMessage = "Enter a valid inventory item ID.";
				return;
			}
			try
			{
				var ok = Bank.DepositFromInventoryById(id, BankInvMenuText);
				StatusMessage = ok ? $"Deposit action sent for ID {id}." : "Deposit action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void BankInvDepositByName()
		{
			if (string.IsNullOrWhiteSpace(BankInvActionName))
			{
				StatusMessage = "Enter a valid inventory item name.";
				return;
			}
			try
			{
				var ok = Bank.DepositFromInventoryByName(BankInvActionName, BankInvMenuText);
				StatusMessage = ok ? $"Deposit action sent for '{BankInvActionName}'." : "Deposit action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private static int[] ParseIds(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();
			var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var list = new System.Collections.Generic.List<int>(parts.Length);
			foreach (var p in parts)
				if (int.TryParse(p.Trim(), out var id))
					list.Add(id);
			return list.ToArray();
		}

		private static string[] ParseNames(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
			return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(x => x.Trim()).ToArray();
		}

		private string TryEnterPin()
		{
			if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 4) return "Enter 4 digits.";
			if (!int.TryParse(PinInput[0].ToString(), out var d1)) return "Invalid PIN.";
			if (!int.TryParse(PinInput[1].ToString(), out var d2)) return "Invalid PIN.";
			if (!int.TryParse(PinInput[2].ToString(), out var d3)) return "Invalid PIN.";
			if (!int.TryParse(PinInput[3].ToString(), out var d4)) return "Invalid PIN.";
			return Bank.EnterPin(d1, d2, d3, d4) ? "PIN sent." : "Failed to send PIN.";
		}

		private string TrySetXQuantity()
		{
			if (!int.TryParse(XQuantityInput, out var qty) || qty <= 0) return "Invalid X quantity.";
			return Bank.SetXQuantity(qty) ? "Set X quantity." : "Failed to set X quantity.";
		}

		private string TrySavePreset()
		{
			if (!int.TryParse(PresetNumberInput, out var number)) return "Invalid preset.";
			return Bank.SavePreset(number) ? "Preset saved." : "Failed to save preset.";
		}

		private string TryLoadPreset()
		{
			if (!int.TryParse(PresetNumberInput, out var number)) return "Invalid preset.";
			return Bank.LoadPreset(number) ? "Preset loaded." : "Failed to load preset.";
		}

		private string TrySelectPresetSettings()
		{
			if (!int.TryParse(PresetSettingsNumberInput, out var preset)) return "Invalid preset.";
			return Bank.PresetSettingsSelectPreset(preset) ? "Preset selected." : "Failed to select preset.";
		}

		private void EquipmentOpen()
		{
			try
			{
				var success = Equipment.OpenInterface();
				StatusMessage = success ? "Equipment interface opened." : "Failed to open equipment.";
				RefreshEquipmentStatus();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		// ─── Status Refresh Methods ─────────────────────────────────────────
			private void RefreshInventoryStatus()
			{
				try
				{
					InventoryIsOpen = Inventory.IsOpen;
					InventoryIsFull = Inventory.IsFull;
					InventoryIsEmpty = Inventory.IsEmpty;
					InventoryItemSelected = Inventory.IsItemSelected;
					InventoryFreeSlots = Inventory.FreeSlots;
				}
				catch { /* ignore */ }
			}

			private void InventoryDoActionById()
			{
				try
				{
					var ok = Inventory.DoAction(InventoryActionItemId, InventoryActionMenuIndex, InventoryActionSelectedOffset?.Value ?? Objects.Offsets.GeneralInterfaceRoute);
					StatusMessage = ok ? "Inventory.DoAction(id): OK" : "Inventory.DoAction(id): Failed";
				}
				catch (Exception ex)
				{
					StatusMessage = $"Inventory.DoAction(id) error: {ex.Message}";
				}
			}

			private void InventoryDoActionByName()
			{
				if (string.IsNullOrWhiteSpace(InventoryActionItemName))
				{
					StatusMessage = "Enter an item name.";
					return;
				}
				try
				{
					var ok = Inventory.DoAction(InventoryActionItemName, InventoryActionMenuIndex, InventoryActionSelectedOffset?.Value ?? Objects.Offsets.GeneralInterfaceRoute);
					StatusMessage = ok ? "Inventory.DoAction(name): OK" : "Inventory.DoAction(name): Failed";
				}
				catch (Exception ex)
				{
					StatusMessage = $"Inventory.DoAction(name) error: {ex.Message}";
				}
			}

			private void InventoryDoActionBySlotFallback()
			{
				try
				{
					var item = Inventory.FindById(InventoryActionItemId).FirstOrDefault();
					if (item == null)
					{
						StatusMessage = "Item not found in inventory.";
						return;
					}

					var ok = InventoryInterfaces.DoActionBySlot(item.Id, item.Slot, InventoryActionMenuIndex, InventoryActionSelectedOffset?.Value ?? Objects.Offsets.GeneralInterfaceRoute);
					StatusMessage = ok ? "InventoryInterfaces.DoActionBySlot: OK" : "InventoryInterfaces.DoActionBySlot: Failed";
				}
				catch (Exception ex)
				{
					StatusMessage = $"InventoryInterfaces.DoActionBySlot error: {ex.Message}";
				}
			}

			private void InventoryRubById()
			{
				try
				{
					var ok = Inventory.Rub(InventoryActionItemId);
					StatusMessage = ok ? "Inventory.Rub(id): OK" : "Inventory.Rub(id): Failed";
				}
				catch (Exception ex)
				{
					StatusMessage = $"Inventory.Rub(id) error: {ex.Message}";
				}
			}

			private void InventoryRubByName()
			{
				if (string.IsNullOrWhiteSpace(InventoryActionItemName))
				{
					StatusMessage = "Enter an item name.";
					return;
				}
				try
				{
					var ok = Inventory.Rub(InventoryActionItemName);
					StatusMessage = ok ? "Inventory.Rub(name): OK" : "Inventory.Rub(name): Failed";
				}
				catch (Exception ex)
				{
					StatusMessage = $"Inventory.Rub(name) error: {ex.Message}";
				}
			}

			private void InventoryResolveRoot()
			{
				try
				{
					var id = InventoryInterfaces.ResolveInventoryRoot();
					StatusMessage = $"Inventory root resolved: id1={id.Id1}, id2={id.Id2}, id3={id.Id3}";
				}
				catch (Exception ex)
				{
					StatusMessage = $"ResolveInventoryRoot error: {ex.Message}";
				}
			}
	
			private void RefreshBankStatus()
			{
				try
				{
				BankIsOpen = Bank.IsOpen;
				BankNoteModeEnabled = Bank.IsNoteModeEnabled();
				BankQuantitySelected = Bank.GetQuantitySelected();
				BankXQuantity = Bank.GetXQuantity();
			}
			catch { /* ignore */ }
		}

		private void RefreshEquipmentStatus()
		{
			try
			{
				EquipmentIsOpen = Equipment.IsOpen();
			}
			catch { /* ignore */ }
		}

		private void RefreshFamiliarStatus()
		{
			try
			{
				HasFamiliar = Familiar.HasFamiliar();
				FamiliarName = Familiar.GetName();
				FamiliarTimeRemaining = Familiar.GetTimeRemaining();
				FamiliarCanRenew = Familiar.CanRenew();
				FamiliarSpellPoints = Familiar.GetSpellPoints();
				FamiliarHealth = Familiar.GetHealth();
			}
			catch { /* ignore */ }
		}

		private void FamiliarCastSpecial()
		{
			try
			{
				var success = Familiar.CastSpecialAttack();
				StatusMessage = success ? "Cast familiar special attack." : "Failed to cast special.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		// ─── Item Action Methods ────────────────────────────────────────────
		private void ItemEat()
		{
			if (SelectedItem == null) return;
			try
			{
				var success = Inventory.Eat(SelectedItem.Id);
				StatusMessage = success ? $"Ate {SelectedItem.Name}." : "Eat action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void ItemDrop()
		{
			if (SelectedItem == null) return;
			try
			{
				var success = Inventory.Drop(SelectedItem.Id);
				StatusMessage = success ? $"Dropped {SelectedItem.Name}." : "Drop action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void ItemUse()
		{
			if (SelectedItem == null) return;
			try
			{
				var success = Inventory.Use(SelectedItem.Id);
				StatusMessage = success ? $"Used {SelectedItem.Name}." : "Use action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void ItemEquip()
		{
			if (SelectedItem == null) return;
			try
			{
				var success = Inventory.Equip(SelectedItem.Id);
				StatusMessage = success ? $"Equipped {SelectedItem.Name}." : "Equip action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void ItemNote()
		{
			if (SelectedItem == null) return;
			try
			{
				var success = Inventory.Note(SelectedItem.Id);
				StatusMessage = success ? $"Noted {SelectedItem.Name}." : "Note action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void ItemUnequip()
		{
			if (SelectedItem == null) return;
			try
			{
				var success = Equipment.UnequipById(SelectedItem.Id);
				StatusMessage = success ? $"Unequipped {SelectedItem.Name}." : "Unequip action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void ItemUseOnItem()
		{
			if (SelectedItem == null || TargetItem == null) return;
			try
			{
				var success = Inventory.UseItemOnItem(SelectedItem.Id, TargetItem.Id);
				StatusMessage = success
					? $"Used {SelectedItem.Name} on {TargetItem.Name}."
					: "UseItemOnItem action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
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
			// Optional: Auto-load when view is activated
		}

		public void OnDeactivated()
		{
			// Cleanup if needed
		}
		#endregion
	}
}
