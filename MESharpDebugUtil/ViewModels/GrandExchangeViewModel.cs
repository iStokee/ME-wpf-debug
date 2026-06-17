using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MESharp.API;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class GrandExchangeSlotViewModel
    {
        public int SlotIndex { get; init; }
        public int SlotNumber => SlotIndex + 1;
        public int Status { get; init; }
        public string StatusLabel { get; init; } = string.Empty;
        public string OrderType { get; init; } = string.Empty;
        public int ItemId { get; init; }
        public int Price { get; init; }
        public int Quantity { get; init; }
        public int CompletedQuantity { get; init; }
        public int CompletedValue { get; init; }
    }

    public class GrandExchangeViewModel : INotifyPropertyChanged, IActivatableViewModel
    {
        private bool _isAtGe;
        public bool IsAtGE
        {
            get => _isAtGe;
            set { _isAtGe = value; OnPropertyChanged(nameof(IsAtGE)); }
        }

        private bool _isWindowOpen;
        public bool IsWindowOpen
        {
            get => _isWindowOpen;
            set { _isWindowOpen = value; OnPropertyChanged(nameof(IsWindowOpen)); }
        }

        private bool _isSearchOpen;
        public bool IsSearchOpen
        {
            get => _isSearchOpen;
            set { _isSearchOpen = value; OnPropertyChanged(nameof(IsSearchOpen)); }
        }

        private int _availableSlots;
        public int AvailableSlots
        {
            get => _availableSlots;
            set { _availableSlots = value; OnPropertyChanged(nameof(AvailableSlots)); }
        }

        private int _finishedSlots;
        public int FinishedSlots
        {
            get => _finishedSlots;
            set { _finishedSlots = value; OnPropertyChanged(nameof(FinishedSlots)); }
        }

        private int _nextAvailableSlot;
        public int NextAvailableSlot
        {
            get => _nextAvailableSlot;
            set { _nextAvailableSlot = value; OnPropertyChanged(nameof(NextAvailableSlot)); }
        }

        private int _delayOffset;
        public int DelayOffset
        {
            get => _delayOffset;
            set { _delayOffset = value; OnPropertyChanged(nameof(DelayOffset)); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        private int _openSlotIndex;
        public int OpenSlotIndex
        {
            get => _openSlotIndex;
            set { _openSlotIndex = value; OnPropertyChanged(nameof(OpenSlotIndex)); }
        }

        private int _cancelSlotIndex;
        public int CancelSlotIndex
        {
            get => _cancelSlotIndex;
            set { _cancelSlotIndex = value; OnPropertyChanged(nameof(CancelSlotIndex)); }
        }

        private int _findItemId;
        public int FindItemId
        {
            get => _findItemId;
            set { _findItemId = value; OnPropertyChanged(nameof(FindItemId)); }
        }

        private int _manualItemId;
        public int ManualItemId
        {
            get => _manualItemId;
            set { _manualItemId = value; OnPropertyChanged(nameof(ManualItemId)); }
        }

        private int _manualPrice;
        public int ManualPrice
        {
            get => _manualPrice;
            set { _manualPrice = value; OnPropertyChanged(nameof(ManualPrice)); }
        }

        private int _manualQuantity;
        public int ManualQuantity
        {
            get => _manualQuantity;
            set { _manualQuantity = value; OnPropertyChanged(nameof(ManualQuantity)); }
        }

        private int _searchItemId;
        public int SearchItemId
        {
            get => _searchItemId;
            set { _searchItemId = value; OnPropertyChanged(nameof(SearchItemId)); }
        }

        private int _placeItemId;
        public int PlaceItemId
        {
            get => _placeItemId;
            set { _placeItemId = value; OnPropertyChanged(nameof(PlaceItemId)); }
        }

        private string _placeItemName = string.Empty;
        public string PlaceItemName
        {
            get => _placeItemName;
            set { _placeItemName = value; OnPropertyChanged(nameof(PlaceItemName)); }
        }

        private int _placePrice;
        public int PlacePrice
        {
            get => _placePrice;
            set { _placePrice = value; OnPropertyChanged(nameof(PlacePrice)); }
        }

        private int _placeQuantity;
        public int PlaceQuantity
        {
            get => _placeQuantity;
            set { _placeQuantity = value; OnPropertyChanged(nameof(PlaceQuantity)); }
        }

        private GrandExchangeOrderType _placeOrderType = GrandExchangeOrderType.Buy;
        public GrandExchangeOrderType PlaceOrderType
        {
            get => _placeOrderType;
            set { _placeOrderType = value; OnPropertyChanged(nameof(PlaceOrderType)); }
        }

        public Array OrderTypes => Enum.GetValues(typeof(GrandExchangeOrderType));

        public ObservableCollection<GrandExchangeSlotViewModel> Slots { get; } = new ObservableCollection<GrandExchangeSlotViewModel>();

        public ICommand RefreshCommand { get; }
        public ICommand OpenGeCommand { get; }
        public ICommand CloseGeCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CollectCommand { get; }
        public ICommand OpenSlotCommand { get; }
        public ICommand OpenNextSlotCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand FindOrderCommand { get; }
        public ICommand ApplyDelayCommand { get; }
        public ICommand PlaceOrderCommand { get; }
        public ICommand SearchItemCommand { get; }
        public ICommand SelectItemCommand { get; }
        public ICommand SetPriceCommand { get; }
        public ICommand SetQuantityCommand { get; }
        public ICommand ConfirmOrderCommand { get; }

        public GrandExchangeViewModel()
        {
            RefreshCommand = new RelayCommand(_ => RefreshAll(), _ => Game.IsInjected);
            OpenGeCommand = new RelayCommand(_ => OpenGe(), _ => Game.IsInjected);
            CloseGeCommand = new RelayCommand(_ => CloseGe(), _ => Game.IsInjected);
            BackCommand = new RelayCommand(_ => Back(), _ => Game.IsInjected);
            CollectCommand = new RelayCommand(_ => Collect(), _ => Game.IsInjected);
            OpenSlotCommand = new RelayCommand(_ => OpenSlot(), _ => Game.IsInjected);
            OpenNextSlotCommand = new RelayCommand(_ => OpenNextSlot(), _ => Game.IsInjected);
            CancelOrderCommand = new RelayCommand(_ => CancelOrder(), _ => Game.IsInjected);
            FindOrderCommand = new RelayCommand(_ => FindOrder(), _ => Game.IsInjected);
            ApplyDelayCommand = new RelayCommand(_ => ApplyDelayOffset(), _ => Game.IsInjected);
            PlaceOrderCommand = new RelayCommand(_ => PlaceOrder(), _ => Game.IsInjected);
            SearchItemCommand = new RelayCommand(_ => SearchItem(), _ => Game.IsInjected);
            SelectItemCommand = new RelayCommand(_ => SelectItem(), _ => Game.IsInjected);
            SetPriceCommand = new RelayCommand(_ => SetPrice(), _ => Game.IsInjected);
            SetQuantityCommand = new RelayCommand(_ => SetQuantity(), _ => Game.IsInjected);
            ConfirmOrderCommand = new RelayCommand(_ => ConfirmOrder(), _ => Game.IsInjected);
        }

        private void RefreshAll()
        {
            if (!Game.IsInjected)
            {
                StatusMessage = "Not injected.";
                return;
            }

            RefreshStatus();
            LoadSlots();
            StatusMessage = "Grand Exchange data refreshed.";
        }

        private void RefreshStatus()
        {
            IsAtGE = GrandExchange.IsAtGE();
            IsWindowOpen = GrandExchange.IsGEWindowOpen();
            IsSearchOpen = GrandExchange.IsGESearchOpen();
            AvailableSlots = GrandExchange.GetAvailableSlots();
            FinishedSlots = GrandExchange.GetFinishedSlots();
            NextAvailableSlot = GrandExchange.GetNextAvailableSlot();
        }

        private void LoadSlots()
        {
            Slots.Clear();
            foreach (var entry in GrandExchange.GetData())
            {
                Slots.Add(new GrandExchangeSlotViewModel
                {
                    SlotIndex = entry.SlotIndex,
                    Status = entry.Status,
                    StatusLabel = StatusToLabel(entry.Status),
                    OrderType = entry.OrderType.ToString(),
                    ItemId = entry.ItemId,
                    Price = entry.Price,
                    Quantity = entry.Quantity,
                    CompletedQuantity = entry.CompletedQuantity,
                    CompletedValue = entry.CompletedValue
                });
            }
        }

        private void OpenGe()
        {
            var ok = GrandExchange.Open();
            StatusMessage = ok ? "Attempted to open GE." : "Failed to open GE.";
            RefreshStatus();
        }

        private void CloseGe()
        {
            GrandExchange.Close();
            StatusMessage = "Sent close command.";
            RefreshStatus();
        }

        private void Back()
        {
            var ok = GrandExchange.Back();
            StatusMessage = ok ? "Back action sent." : "Back action failed.";
            RefreshStatus();
        }

        private void Collect()
        {
            var ok = GrandExchange.CollectToInventory();
            StatusMessage = ok ? "Collect action sent." : "Collect action failed.";
            RefreshStatus();
        }

        private void OpenSlot()
        {
            var ok = GrandExchange.OpenSlot(OpenSlotIndex);
            StatusMessage = ok ? $"Opened slot {OpenSlotIndex + 1}." : "Failed to open slot.";
            RefreshStatus();
        }

        private void OpenNextSlot()
        {
            var ok = GrandExchange.OpenNextAvailableSlot();
            StatusMessage = ok ? "Opened next available slot." : "No available slots.";
            RefreshStatus();
        }

        private void CancelOrder()
        {
            var ok = GrandExchange.CancelOrder(CancelSlotIndex - 1);
            StatusMessage = ok ? $"Cancel sent for slot {CancelSlotIndex}." : "Cancel failed.";
            RefreshStatus();
        }

        private void FindOrder()
        {
            var slotIndex = GrandExchange.FindOrder(FindItemId);
            StatusMessage = slotIndex >= 0
                ? $"Item {FindItemId} is in slot {slotIndex + 1}."
                : "Order not found.";
        }

        private void ApplyDelayOffset()
        {
            GrandExchange.DelayOffset(DelayOffset);
            StatusMessage = $"Delay offset set to {DelayOffset} ms.";
        }

        private void PlaceOrder()
        {
            var ok = GrandExchange.PlaceOrder(PlaceOrderType, PlaceItemId, PlaceItemName ?? string.Empty, PlacePrice, PlaceQuantity);
            StatusMessage = ok ? "Place order invoked." : "Place order failed.";
            RefreshStatus();
        }

        private void SearchItem()
        {
            var index = GrandExchange.SearchForItemInUI(SearchItemId);
            StatusMessage = index >= 0
                ? $"Item {SearchItemId} found at index {index}."
                : "Item not found in search UI.";
        }

        private void SelectItem()
        {
            var ok = GrandExchange.SelectItem(ManualItemId);
            StatusMessage = ok ? $"Selected item {ManualItemId}." : "Select item failed.";
        }

        private void SetPrice()
        {
            var ok = GrandExchange.SetPrice(ManualPrice);
            StatusMessage = ok ? $"Set price to {ManualPrice}." : "Set price failed.";
        }

        private void SetQuantity()
        {
            var ok = GrandExchange.SetQuantity(ManualQuantity);
            StatusMessage = ok ? $"Set quantity to {ManualQuantity}." : "Set quantity failed.";
        }

        private void ConfirmOrder()
        {
            var ok = GrandExchange.ConfirmOrder();
            StatusMessage = ok ? "Confirm order sent." : "Confirm order failed.";
        }

        private static string StatusToLabel(int status)
        {
            return status switch
            {
                0 => "Empty",
                1 => "Pending",
                2 => "In Progress",
                3 => "Completed",
                4 => "Collectable",
                5 => "Finished",
                _ => $"Unknown ({status})"
            };
        }

        public void OnActivated()
        {
            StatusMessage = "Ready. Refresh to sync slots.";
            OpenSlotIndex = 0;
            CancelSlotIndex = 1;
            FindItemId = 0;
            SearchItemId = 0;
            ManualItemId = 0;
            ManualPrice = 0;
            ManualQuantity = 0;
            PlaceItemId = 0;
            PlaceItemName = string.Empty;
            PlacePrice = 0;
            PlaceQuantity = 0;
            RefreshStatus();
            LoadSlots();
        }

        public void OnDeactivated()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
