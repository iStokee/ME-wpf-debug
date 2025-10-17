using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MESharp.ViewModels
{
	public class ItemsUnifiedViewModel : INotifyPropertyChanged, IActivatableViewModel
	{
		// ─── State ───────────────────────────────────────────────────────────
		private ContainerType _selectedContainer = ContainerType.Inventory;
		private bool _includeCoordinates;

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

		// ─── Commands ────────────────────────────────────────────────────────
		public ICommand LoadItemsCommand { get; }
		public ICommand ClearCommand { get; }

		// Container-specific commands (placeholders for now)
		public ICommand InventoryRefreshCommand { get; }
		public ICommand BankDepositAllCommand { get; }
		public ICommand BankDepositExceptIdsCommand { get; }
		public ICommand EquipmentRefreshCommand { get; }
		public ICommand LootRefreshCommand { get; }
		public ICommand MaterialCacheRefreshCommand { get; }
		public ICommand TradeWindowRefreshCommand { get; }

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

		// ─── Bank-specific properties ───────────────────────────────────────
		private string _bankKeepIds = string.Empty;
		public string BankKeepIds
		{
			get => _bankKeepIds;
			set => SetProperty(ref _bankKeepIds, value);
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

			// Container-specific commands (implement as needed)
			InventoryRefreshCommand = new RelayCommand(_ => LoadItems());
			BankDepositAllCommand = new RelayCommand(_ => BankDepositAll());
			BankDepositExceptIdsCommand = new RelayCommand(_ => BankDepositExceptIds());
			EquipmentRefreshCommand = new RelayCommand(_ => LoadItems());
			LootRefreshCommand = new RelayCommand(_ => LoadItems());
			MaterialCacheRefreshCommand = new RelayCommand(_ => LoadItems());
			TradeWindowRefreshCommand = new RelayCommand(_ => LoadItems());

			StatusMessage = "Select a container and click Load Items.";
			UpdateContainerVisibility();
		}

		private void UpdateContainerVisibility()
		{
			IsInventorySelected = SelectedContainer == ContainerType.Inventory;
			IsBankSelected = SelectedContainer == ContainerType.Bank;
			IsEquipmentSelected = SelectedContainer == ContainerType.Equipment;
			IsLootSelected = SelectedContainer == ContainerType.Loot;
			IsMaterialCacheSelected = SelectedContainer == ContainerType.MaterialCache;
			IsTradeWindowSelected = SelectedContainer == ContainerType.TradeWindow;
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
