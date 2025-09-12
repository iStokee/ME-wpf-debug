using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Data;

namespace MESharp.ViewModels
{
	public enum InventoryAction { Eat, Drop, Use, Equip, Note }

	public class InventoryViewModel : INotifyPropertyChanged
	{
		// ─── State ───────────────────────────────────────────────────────────
		private bool _isOpen, _isFull, _isEmpty, _isItemSelected;
		private int _freeSlots;
		public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }
		public bool IsFull { get => _isFull; set => SetProperty(ref _isFull, value); }
		public bool IsEmpty { get => _isEmpty; set => SetProperty(ref _isEmpty, value); }
		public bool IsItemSelected { get => _isItemSelected; set => SetProperty(ref _isItemSelected, value); }
		public int FreeSlots { get => _freeSlots; set => SetProperty(ref _freeSlots, value); }

		public ICommand RefreshStateCommand { get; }

		// ─── Collections ─────────────────────────────────────────────────────
		public ObservableCollection<Inventory.Item> AllItems { get; } = new ObservableCollection<Inventory.Item>();
		public ICollectionView ItemsView { get; }
		public ObservableCollection<Inventory.Item> FindByIdResults { get; }
			= new ObservableCollection<Inventory.Item>();
		public ObservableCollection<Inventory.Item> FindByNameResults { get; }
			= new ObservableCollection<Inventory.Item>();

		public ICommand LoadAllCommand { get; }
		public ICommand FindByIdCommand { get; }
		public ICommand FindByNameCommand { get; }

		// ─── Contains / Count ─────────────────────────────────────────────────
		private string _containsIdResult, _containsAnyResult, _containsAllResult;
		private string _countOfIdResult, _countOfNameResult;
		public string ContainsIdResult { get => _containsIdResult; set => SetProperty(ref _containsIdResult, value); }
		public string ContainsAnyResult { get => _containsAnyResult; set => SetProperty(ref _containsAnyResult, value); }
		public string ContainsAllResult { get => _containsAllResult; set => SetProperty(ref _containsAllResult, value); }
		public string CountOfIdResult { get => _countOfIdResult; set => SetProperty(ref _countOfIdResult, value); }
		public string CountOfNameResult { get => _countOfNameResult; set => SetProperty(ref _countOfNameResult, value); }

		public ICommand ContainsIdCommand { get; }
		public ICommand ContainsAnyCommand { get; }
		public ICommand ContainsAllCommand { get; }
		public ICommand CountOfIdCommand { get; }
		public ICommand CountOfNameCommand { get; }

		// ─── Quick Filter (unified search) ─────────────────────────────────────
		private string _quickFilterText;
		private bool _quickIsId, _quickContains, _treatAsIds;
		private int _quickCount;
		private bool _containsAnyFlag, _containsAllFlag;
		public string QuickFilterText { get => _quickFilterText; set { if (SetProperty(ref _quickFilterText, value)) { ItemsView?.Refresh(); UpdateQuickFilter(); } } }
		public bool QuickIsId { get => _quickIsId; private set => SetProperty(ref _quickIsId, value); }
		public bool QuickContains { get => _quickContains; private set => SetProperty(ref _quickContains, value); }
		public int QuickCount { get => _quickCount; private set { if (SetProperty(ref _quickCount, value)) OnPropertyChanged(nameof(HasCount)); } }
		public bool HasCount => QuickCount > 0;
		public bool TreatAsIds { get => _treatAsIds; set { if (SetProperty(ref _treatAsIds, value)) { ItemsView?.Refresh(); UpdateQuickFilter(); } } }
		public bool ContainsAnyFlag { get => _containsAnyFlag; private set => SetProperty(ref _containsAnyFlag, value); }
		public bool ContainsAllFlag { get => _containsAllFlag; private set => SetProperty(ref _containsAllFlag, value); }
		public ObservableCollection<Inventory.Item> QuickFindResults { get; } = new();

		// ─── Inputs ────────────────────────────────────────────────────────────
        private string _idInput, _nameInput, _containsIdsInput, _containsAnyIdsInput;
        public string IdInput { get => _idInput; set => SetProperty(ref _idInput, value); }
        public string NameInput { get => _nameInput; set => SetProperty(ref _nameInput, value); }
        public string ContainsIdsInput { get => _containsIdsInput; set => SetProperty(ref _containsIdsInput, value); }
        public string ContainsAnyIdsInput { get => _containsAnyIdsInput; set => SetProperty(ref _containsAnyIdsInput, value); }

		// ─── Actions ───────────────────────────────────────────────────────────
		public InventoryAction[] ActionTypes =>
			(InventoryAction[])Enum.GetValues(typeof(InventoryAction));

		private InventoryAction _selectedAction;
		public InventoryAction SelectedAction
		{
			get => _selectedAction;
			set => SetProperty(ref _selectedAction, value);
		}

		private bool _useIdForAction;
		private string _actionInput, _actionResult;
		public bool UseIdForAction { get => _useIdForAction; set => SetProperty(ref _useIdForAction, value); }
		public string ActionInput { get => _actionInput; set => SetProperty(ref _actionInput, value); }
		public string ActionResult { get => _actionResult; set => SetProperty(ref _actionResult, value); }

		public ICommand ExecuteActionCommand { get; }

		public InventoryViewModel()
		{
		// Hook collection view for filtering
		ItemsView = CollectionViewSource.GetDefaultView(AllItems);
		ItemsView.Filter = ItemFilter;

		// State
			RefreshStateCommand = new RelayCommand(_ => {
				IsOpen         = Inventory.IsOpen;
				IsFull         = Inventory.IsFull;
				IsEmpty        = Inventory.IsEmpty;
				IsItemSelected = Inventory.IsItemSelected;
				FreeSlots      = Inventory.FreeSlots;
			});

			// Load / Find
			LoadAllCommand    = new RelayCommand(_ => {
				AllItems.Clear();
				foreach (var it in Inventory.GetAll()) AllItems.Add(it);
				ItemsView.Refresh();
			});
			FindByIdCommand   = new RelayCommand(_ => {
				FindByIdResults.Clear();
				if (int.TryParse(IdInput, out var id))
					foreach (var it in Inventory.FindById(id))
						FindByIdResults.Add(it);
			});
			FindByNameCommand = new RelayCommand(_ => {
				FindByNameResults.Clear();
				foreach (var it in Inventory.FindByName(NameInput))
					FindByNameResults.Add(it);
			});

			// Contains / Count
			ContainsIdCommand  = new RelayCommand(_ => {
				if (int.TryParse(IdInput, out var id))
					ContainsIdResult = Inventory.ContainsId(id).ToString();
			});
			ContainsAnyCommand = new RelayCommand(_ => {
				var ids = ContainsIdsInput
						  .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
						  .Select(s => int.TryParse(s.Trim(), out var x) ? x : (int?)null)
						  .Where(x => x!=null).Select(x => x.Value).ToArray();
				ContainsAnyResult = Inventory.ContainsAny(ids).ToString();
			});
			ContainsAllCommand = new RelayCommand(_ => {
				var ids = ContainsIdsInput
						  .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
						  .Select(s => int.TryParse(s.Trim(), out var x) ? x : (int?)null)
						  .Where(x => x!=null).Select(x => x.Value).ToArray();
				ContainsAllResult = Inventory.ContainsAll(ids).ToString();
			});
			CountOfIdCommand   = new RelayCommand(_ => {
				if (int.TryParse(IdInput, out var id))
					CountOfIdResult = Inventory.CountOf(id).ToString();
			});
			CountOfNameCommand = new RelayCommand(_ => {
				CountOfNameResult = Inventory.CountOf(NameInput).ToString();
			});

			// Execute Eat/Drop/Use/Equip/Note
			ExecuteActionCommand = new RelayCommand(_ => {
				bool ok = false;
				if (UseIdForAction && int.TryParse(ActionInput, out var id))
				{
					switch (SelectedAction)
					{
						case InventoryAction.Eat: ok = Inventory.Eat(id); break;
						case InventoryAction.Drop: ok = Inventory.Drop(id); break;
						case InventoryAction.Use: ok = Inventory.Use(id); break;
						case InventoryAction.Equip: ok = Inventory.Equip(id); break;
						case InventoryAction.Note: ok = Inventory.Note(id); break;
					}
				}
				else
				{
					var name = ActionInput;
					switch (SelectedAction)
					{
						case InventoryAction.Eat: ok = Inventory.Eat(name); break;
						case InventoryAction.Drop: ok = Inventory.Drop(name); break;
						case InventoryAction.Use: ok = Inventory.Use(name); break;
						case InventoryAction.Equip: ok = Inventory.Equip(name); break;
						case InventoryAction.Note: ok = Inventory.Note(name); break;
					}
				}
				ActionResult = ok ? "✔ Success" : "✘ Failed";
			});

			// Initialize derived quick filter state
			UpdateQuickFilter();
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
		#endregion

		private void UpdateQuickFilter()
		{
			QuickFindResults.Clear();
			QuickContains = false;
			QuickCount = 0;
			QuickIsId = false;

			var q = QuickFilterText;
			if (string.IsNullOrWhiteSpace(q))
			{
				ActionInput = string.Empty;
				ItemsView?.Refresh();
				return;
			}

			var ids = ParseIds(q);
			bool idsMode = TreatAsIds || ids.Length > 0;
			if (idsMode)
			{
				QuickIsId = true;
				IdInput = q;
				UseIdForAction = true; ActionInput = q;

				// Populate quick find results and contains/count
				FindByIdResults.Clear();
				foreach (var id in ids)
					foreach (var it in Inventory.FindById(id)) QuickFindResults.Add(it);
				foreach (var it in QuickFindResults) FindByIdResults.Add(it);

				if (ids.Length == 1)
				{
					var single = ids[0];
					QuickContains = Inventory.ContainsId(single);
					ContainsIdResult = QuickContains.ToString();
					QuickCount = (int)Inventory.CountOf(single);
					CountOfIdResult = QuickCount.ToString();
				}
				ContainsAnyFlag = Inventory.ContainsAny(ids);
				ContainsAllFlag = Inventory.ContainsAll(ids);
				ContainsAnyResult = ContainsAnyFlag.ToString();
				ContainsAllResult = ContainsAllFlag.ToString();
			}
			else
			{
				QuickIsId = false;
				NameInput = q;
				foreach (var it in Inventory.FindByName(q)) QuickFindResults.Add(it);
				FindByNameResults.Clear();
				foreach (var it in QuickFindResults) FindByNameResults.Add(it);

				QuickCount = (int)Inventory.CountOf(q);
				CountOfNameResult = QuickCount.ToString();
				QuickContains = QuickCount > 0;
				ContainsAnyFlag = ContainsAllFlag = false;
			}

			ItemsView?.Refresh();
		}

		private static int[] ParseIds(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();
			var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var list = new System.Collections.Generic.List<int>(parts.Length);
			foreach (var p in parts)
				if (int.TryParse(p.Trim(), out var id)) list.Add(id);
			return list.ToArray();
		}

		private bool ItemFilter(object o)
		{
			if (string.IsNullOrWhiteSpace(QuickFilterText)) return true;
			var it = (Inventory.Item)o;
			var ids = ParseIds(QuickFilterText);
			var idsMode = TreatAsIds || ids.Length > 0;
			if (idsMode) return ids.Contains(it.Id);
			return it.Name?.IndexOf(QuickFilterText, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private void OnPropertyChanged(string v)
		{
			if (string.IsNullOrEmpty(v)) return;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
		}

	}
}
