using MESharp.API;
using MESharp.Commands;
using System;
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
		public ICommand BankDepositAllCommand { get; }
		public ICommand BankDepositExceptIdsCommand { get; }
		public ICommand BankCloseCommand { get; }
		public ICommand BankWithdrawSelectedCommand { get; }
		public ICommand BankWithdrawByIdCommand { get; }
		public ICommand BankWithdrawByNameCommand { get; }
		public ICommand BankInvDepositSelectedCommand { get; }
		public ICommand BankInvDepositByIdCommand { get; }
		public ICommand BankInvDepositByNameCommand { get; }
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

		private bool _bankIsOpen;
		public bool BankIsOpen
		{
			get => _bankIsOpen;
			set => SetProperty(ref _bankIsOpen, value);
		}

		private bool _equipmentIsOpen;
		public bool EquipmentIsOpen
		{
			get => _equipmentIsOpen;
			set => SetProperty(ref _equipmentIsOpen, value);
		}

		public ItemsUnifiedViewModel()
		{
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
			BankDepositAllCommand = new RelayCommand(_ => BankDepositAll());
			BankDepositExceptIdsCommand = new RelayCommand(_ => BankDepositExceptIds());
			BankCloseCommand = new RelayCommand(_ => BankClose());
			BankWithdrawSelectedCommand = new RelayCommand(_ => BankWithdrawSelected());
			BankWithdrawByIdCommand = new RelayCommand(_ => BankWithdrawById());
			BankWithdrawByNameCommand = new RelayCommand(_ => BankWithdrawByName());
			BankInvDepositSelectedCommand = new RelayCommand(_ => BankInvDepositSelected());
			BankInvDepositByIdCommand = new RelayCommand(_ => BankInvDepositById());
			BankInvDepositByNameCommand = new RelayCommand(_ => BankInvDepositByName());
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

		private void RefreshBankStatus()
		{
			try
			{
				BankIsOpen = Bank.IsOpen;
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
