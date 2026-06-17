using MESharp.API;
using MESharp.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public class TradeWindowViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ItemContainer> _items = new();
        public ObservableCollection<ItemContainer> Items { get => _items; set => Set(ref _items, value); }

        private int _itemCount;
        public int ItemCount { get => _itemCount; set => Set(ref _itemCount, value); }

        public ICommand LoadItemsCommand { get; }
        public ICommand ClearCommand { get; }

        public TradeWindowViewModel()
        {
            LoadItemsCommand = new RelayCommand(_ => LoadItems());
            ClearCommand = new RelayCommand(_ => Items.Clear());
        }

        private void LoadItems()
        {
            var items = TradeWindow.GetItems(includeCoordinates: false);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
            ItemCount = Items.Count;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return true;
            }
            return false;
        }
    }
}
