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
	public enum GameObjectType
	{
		NPC,
		Object
	}

	public class ObjectsUnifiedViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
	{
		private readonly DispatcherTimer _updateTimer;
		private bool _isActive;
		private bool _disposed;

		// ─── State ───────────────────────────────────────────────────────────
		private GameObjectType _selectedObjectType = GameObjectType.NPC;
		public GameObjectType SelectedObjectType
		{
			get => _selectedObjectType;
			set
			{
				if (SetProperty(ref _selectedObjectType, value))
				{
					UpdateObjectTypeVisibility();
					LoadObjects();
				}
			}
		}

		public GameObjectType[] AvailableObjectTypes => (GameObjectType[])Enum.GetValues(typeof(GameObjectType));

		// ─── Filter ──────────────────────────────────────────────────────────
		private string _filterText = string.Empty;
		public string FilterText
		{
			get => _filterText;
			set
			{
				if (SetProperty(ref _filterText, value))
				{
					ObjectsView?.Refresh();
				}
			}
		}

		// ─── Collections ─────────────────────────────────────────────────────
		public ObservableCollection<Objects.GameObject> AllObjects { get; } = new ObservableCollection<Objects.GameObject>();
		public ICollectionView ObjectsView { get; private set; }

		private Objects.GameObject _selectedObject;
		public Objects.GameObject SelectedObject
		{
			get => _selectedObject;
			set => SetProperty(ref _selectedObject, value);
		}

		// ─── Commands ────────────────────────────────────────────────────────
		public ICommand LoadObjectsCommand { get; }
		public ICommand ClearCommand { get; }
		public ICommand DoActionCommand { get; }

		// ─── Stats ───────────────────────────────────────────────────────────
		private int _objectCount;
		public int ObjectCount
		{
			get => _objectCount;
			set => SetProperty(ref _objectCount, value);
		}

		private string _statusMessage;
		public string StatusMessage
		{
			get => _statusMessage;
			set => SetProperty(ref _statusMessage, value);
		}

		public bool HasObjects => AllObjects.Count > 0;

		// ─── Type-specific visibility ───────────────────────────────────────
		private bool _isNpcSelected;
		public bool IsNpcSelected
		{
			get => _isNpcSelected;
			set => SetProperty(ref _isNpcSelected, value);
		}

		private bool _isObjectsSelected;
		public bool IsObjectsSelected
		{
			get => _isObjectsSelected;
			set => SetProperty(ref _isObjectsSelected, value);
		}

		// ─── NPC-specific properties ────────────────────────────────────────
		private bool _showOnlyAlive;
		public bool ShowOnlyAlive
		{
			get => _showOnlyAlive;
			set
			{
				if (SetProperty(ref _showOnlyAlive, value))
				{
					ObjectsView?.Refresh();
				}
			}
		}

		private bool _autoRefresh;
		public bool AutoRefresh
		{
			get => _autoRefresh;
			set
			{
				if (SetProperty(ref _autoRefresh, value))
				{
					UpdateTimer();
				}
			}
		}

		// ─── Objects-specific properties ────────────────────────────────────
		private bool _onlyInteractable;
		public bool OnlyInteractable
		{
			get => _onlyInteractable;
			set
			{
				if (SetProperty(ref _onlyInteractable, value))
				{
					ObjectsView?.Refresh();
				}
			}
		}

		// ─── Action properties ──────────────────────────────────────────────
		private int _selectedActionIndex;
		public int SelectedActionIndex
		{
			get => _selectedActionIndex;
			set => SetProperty(ref _selectedActionIndex, value);
		}

		public int[] ActionIndices { get; } = Enumerable.Range(0, 11).ToArray();

		public ObjectsUnifiedViewModel()
		{
			ObjectsView = CollectionViewSource.GetDefaultView(AllObjects);
			ObjectsView.Filter = FilterPredicate;

			LoadObjectsCommand = new RelayCommand(_ => LoadObjects());
			ClearCommand = new RelayCommand(_ =>
			{
				AllObjects.Clear();
				SelectedObject = null;
				ObjectCount = 0;
				StatusMessage = "Cleared.";
				OnPropertyChanged(nameof(HasObjects));
			});
			DoActionCommand = new RelayCommand(_ => DoAction(), _ => CanDoAction());

			_updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			_updateTimer.Tick += OnTimerTick;

			StatusMessage = "Select a type and click Load.";
			UpdateObjectTypeVisibility();
		}

		private void UpdateObjectTypeVisibility()
		{
			IsNpcSelected = SelectedObjectType == GameObjectType.NPC;
			IsObjectsSelected = SelectedObjectType == GameObjectType.Object;
		}

		private void LoadObjects()
		{
			try
			{
				AllObjects.Clear();
				SelectedObject = null;

				if (SelectedObjectType == GameObjectType.NPC)
				{
					// Get all objects and filter to NPCs only
					var allObjects = Objects.GetAll();
					var npcs = allObjects.Where(o => o.Type == (int)Objects.ObjectKind.Npc).ToList();
					foreach (var npc in npcs)
					{
						AllObjects.Add(npc);
					}
				}
				else if (SelectedObjectType == GameObjectType.Object)
				{
					var objects = Objects.GetAll();
					var filtered = objects.Where(o => o.Type == (int)Objects.ObjectKind.Object).ToList();
					foreach (var obj in filtered)
					{
						AllObjects.Add(obj);
					}
				}

				ObjectCount = AllObjects.Count;
				StatusMessage = $"Loaded {ObjectCount} {SelectedObjectType}(s).";
				OnPropertyChanged(nameof(HasObjects));
				ObjectsView?.Refresh();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private bool FilterPredicate(object obj)
		{
			if (obj is not Objects.GameObject go)
				return false;

			// NPC-specific filters
			if (IsNpcSelected && ShowOnlyAlive && go.Life <= 0)
				return false;

			// Objects-specific filters
			if (IsObjectsSelected && OnlyInteractable && string.IsNullOrWhiteSpace(go.Action))
				return false;

			// Text filter
			if (string.IsNullOrWhiteSpace(FilterText))
				return true;

			var token = FilterText.Trim();
			if (int.TryParse(token, out var id))
				return go.Id == id;

			return go.Name?.Contains(token, StringComparison.OrdinalIgnoreCase) == true;
		}

		private bool CanDoAction()
		{
			return !string.IsNullOrWhiteSpace(FilterText) || SelectedObject != null;
		}

		private void DoAction()
		{
			try
			{
				bool ok = false;
				var token = FilterText?.Trim() ?? string.Empty;

				if (!string.IsNullOrWhiteSpace(token))
				{
					if (int.TryParse(token, out var id))
					{
						if (IsNpcSelected)
						{
							ok = Npcs.DoActionByIds(new[] { id }, SelectedActionIndex);
						}
						else
						{
							ok = Objects.DoActionByIds(new[] { id }, SelectedActionIndex);
						}
					}
					else
					{
						if (IsNpcSelected)
						{
							ok = Npcs.DoActionByNames(new[] { token }, SelectedActionIndex);
						}
						else
						{
							ok = Objects.DoActionByNames(new[] { token }, SelectedActionIndex);
						}
					}
				}
				else if (SelectedObject != null)
				{
					ok = SelectedObject.DoAction(SelectedActionIndex);
				}

				StatusMessage = ok ? "Action executed." : "Action failed.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error: {ex.Message}";
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if (_disposed || !_isActive)
				return;

			LoadObjects();
		}

		private void UpdateTimer()
		{
			if (_disposed)
				return;

			if (!_isActive)
			{
				if (_updateTimer.IsEnabled)
					_updateTimer.Stop();
				return;
			}

			if (AutoRefresh)
			{
				if (!_updateTimer.IsEnabled)
					_updateTimer.Start();
			}
			else if (_updateTimer.IsEnabled)
			{
				_updateTimer.Stop();
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
			if (_disposed)
				return;

			_isActive = true;
			UpdateTimer();
		}

		public void OnDeactivated()
		{
			if (_disposed || !_isActive)
				return;

			_isActive = false;
			try { _updateTimer.Stop(); } catch { /* ignore */ }
		}
		#endregion

		#region IDisposable
		public void Dispose()
		{
			if (_disposed)
				return;

			OnDeactivated();
			_disposed = true;

			try
			{
				_updateTimer.Tick -= OnTimerTick;
			}
			catch { /* ignore */ }
		}
		#endregion
	}
}
