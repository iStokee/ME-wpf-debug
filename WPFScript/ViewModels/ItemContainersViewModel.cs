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
	public class ItemContainersViewModel : INotifyPropertyChanged, IActivatableViewModel
	{
		// ─── State ───────────────────────────────────────────────────────────
		private ContainerType _selectedContainer = ContainerType.Inventory;
		private bool _includeCoordinates;

		public ContainerType SelectedContainer
		{
			get => _selectedContainer;
			set => SetProperty(ref _selectedContainer, value);
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

		public ItemContainersViewModel()
		{
			LoadItemsCommand = new RelayCommand(_ => LoadItems());
			ClearCommand = new RelayCommand(_ => {
				Items.Clear();
				SelectedItem = null;
				ItemCount = 0;
				StatusMessage = "Cleared.";
			});

			StatusMessage = "Select a container and click Load Items.";
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
