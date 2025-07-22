using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
	public class NpcViewModel : INotifyPropertyChanged
	{
		private readonly DispatcherTimer _updateTimer;

		public ObservableCollection<Npcs.Npc> AllNpcs { get; }
		public ICollectionView NpcsView { get; }

		private bool _showOnlyAlive;
		public bool ShowOnlyAlive
		{
			get => _showOnlyAlive;
			set
			{
				if (_showOnlyAlive == value) return;
				_showOnlyAlive = value;
				OnPropertyChanged();
				NpcsView.Refresh();
				CommandManager.InvalidateRequerySuggested();
			}
		}

		private string _filterText = "";
		public string FilterText
		{
			get => _filterText;
			set
			{
				if (_filterText == value) return;
				_filterText = value;
				OnPropertyChanged();
				NpcsView.Refresh();
				CommandManager.InvalidateRequerySuggested();
			}
		}

		private int _actionIndex;
		public int ActionIndex
		{
			get => _actionIndex;
			set
			{
				if (_actionIndex == value) return;
				_actionIndex = value;
				OnPropertyChanged();
			}
		}

		public IEnumerable<int> ActionIndices { get; } = Enumerable.Range(0, 11);

		private Npcs.Npc _selectedNpc;
		public Npcs.Npc SelectedNpc
		{
			get => _selectedNpc;
			set
			{
				if (_selectedNpc == value) return;
				_selectedNpc = value;
				OnPropertyChanged();
				CommandManager.InvalidateRequerySuggested();
			}
		}

		public ICommand DoActionCommand { get; }

		public NpcViewModel()
		{
			AllNpcs = new ObservableCollection<Npcs.Npc>(Npcs.GetAll());
			NpcsView = CollectionViewSource.GetDefaultView(AllNpcs);
			NpcsView.Filter = FilterPredicate;

			DoActionCommand = new RelayCommand(_ => ExecuteDoAction(),
											   _ => !string.IsNullOrWhiteSpace(FilterText) || SelectedNpc != null);

			_updateTimer = new DispatcherTimer(
				TimeSpan.FromSeconds(1),
				DispatcherPriority.Background,
				(s, e) => RefreshAll(),
				Dispatcher.CurrentDispatcher);
			_updateTimer.Start();
		}

		private bool FilterPredicate(object o)
		{
			var npc = (Npcs.Npc)o;
			if (ShowOnlyAlive && npc.Health <= 0) return false;
			if (string.IsNullOrWhiteSpace(FilterText)) return true;
			return int.TryParse(FilterText, out var id)
				? npc.Id == id
				: npc.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
		}

		private void RefreshAll()
		{
			var latest = Npcs.GetAll();
			AllNpcs.Clear();
			foreach (var n in latest) AllNpcs.Add(n);
			NpcsView.Refresh();
		}

		private void ExecuteDoAction()
		{
			bool ok = false;

			// 1) Try filterText first:
			if (!string.IsNullOrWhiteSpace(FilterText))
			{
				if (int.TryParse(FilterText, out var id))
				{
					ok = Npcs.DoActionByIds(new[] { id }, ActionIndex);
				}
				else
				{
					ok = Npcs.DoActionByNames(new[] { FilterText }, ActionIndex);
				}
			}
			// 2) Otherwise selected row:
			else if (SelectedNpc != null)
			{
				ok = SelectedNpc.DoAction(ActionIndex);
			}

			// TODO: expose `ok` success/failure to UI if you like
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
