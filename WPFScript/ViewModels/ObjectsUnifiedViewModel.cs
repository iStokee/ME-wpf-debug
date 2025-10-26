using MESharp.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MESharp.Commands;

namespace MESharp.ViewModels
{
	public sealed class GameObjectTypeOption : INotifyPropertyChanged
	{
		private bool _isSelected;

		public GameObjectTypeOption(GameObjectType type, bool isSelected = false)
		{
			Type = type;
			_isSelected = isSelected;
		}

		public GameObjectType Type { get; }

		public string DisplayName => Type switch
		{
			GameObjectType.All => "All",
			GameObjectType.Object => "Objects",
			GameObjectType.NPC => "NPCs",
			GameObjectType.Player => "Players",
			GameObjectType.GroundItem => "Ground Items",
			GameObjectType.Highlight => "Highlights",
			GameObjectType.Projectile => "Projectiles",
			GameObjectType.Tile => "Tiles",
			GameObjectType.Object12 => "Objects (12)",
			_ => Type.ToString()
		};

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public sealed class ActionOffsetOption
	{
		public ActionOffsetOption(ActionOffsets.Offset offset)
		{
			Offset = offset;
		}

		public ActionOffsets.Offset Offset { get; }
		public string DisplayName => Offset.Label;
		public int Value => Offset.Value;
		public string Description => Offset.Description;
		public ActionOffsets.OffsetCategory Category => Offset.Category;
		public override string ToString() => DisplayName;
	}

	public enum GameObjectType
	{
		All,
		Object,
		NPC,
		Player,
		GroundItem,
		Highlight,
		Projectile,
		Tile,
		Object12
	}

	public class ObjectsUnifiedViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
	{
		private readonly DispatcherTimer _updateTimer;
		private bool _isActive;
		private bool _disposed;
		private readonly List<ActionOffsetOption> _allOffsetOptions;

		// ─── State ───────────────────────────────────────────────────────────
		private bool _isSidePanelCollapsed;
		public bool IsSidePanelCollapsed
		{
			get => _isSidePanelCollapsed;
			set => SetProperty(ref _isSidePanelCollapsed, value);
		}

		private bool _suppressTypeOptionNotifications;
		public ObservableCollection<GameObjectTypeOption> ObjectTypeOptions { get; }

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
					CommandManager.InvalidateRequerySuggested();
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

		public ObservableCollection<ActionOffsetOption> OffsetOptions { get; } = new ObservableCollection<ActionOffsetOption>();

		private ActionOffsetOption _selectedOffset;
		public ActionOffsetOption SelectedOffset
		{
			get => _selectedOffset;
			set => SetProperty(ref _selectedOffset, value);
		}

		public ObservableCollection<Npcs.Npc> NpcSearchResults { get; } = new ObservableCollection<Npcs.Npc>();

		private Npcs.Npc _selectedNpcSearchResult;
		public Npcs.Npc SelectedNpcSearchResult
		{
			get => _selectedNpcSearchResult;
			set => SetProperty(ref _selectedNpcSearchResult, value);
		}

		private string _npcLookupStatus = string.Empty;
		public string NpcLookupStatus
		{
			get => _npcLookupStatus;
			set => SetProperty(ref _npcLookupStatus, value);
		}

		private int _npcSnapshotCount;
		public int NpcSnapshotCount
		{
			get => _npcSnapshotCount;
			set => SetProperty(ref _npcSnapshotCount, value);
		}

		public bool HasNpcSearchResults => NpcSearchResults.Count > 0;

		// ─── Commands ────────────────────────────────────────────────────────
		public ICommand LoadObjectsCommand { get; }
		public ICommand SearchByNameCommand { get; }
		public ICommand ClearCommand { get; }
		public ICommand DoActionCommand { get; }
		public ICommand ToggleSidePanelCommand { get; }
		public ICommand LookupNpcByIdCommand { get; }

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
			set
			{
				if (SetProperty(ref _isNpcSelected, value))
				{
					CommandManager.InvalidateRequerySuggested();
				}
			}
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
			ObjectTypeOptions = new ObservableCollection<GameObjectTypeOption>
			{
				new GameObjectTypeOption(GameObjectType.All),
				new GameObjectTypeOption(GameObjectType.Object),
				new GameObjectTypeOption(GameObjectType.NPC, true),
				new GameObjectTypeOption(GameObjectType.Player),
				new GameObjectTypeOption(GameObjectType.GroundItem),
				new GameObjectTypeOption(GameObjectType.Highlight),
				new GameObjectTypeOption(GameObjectType.Projectile),
				new GameObjectTypeOption(GameObjectType.Tile),
				new GameObjectTypeOption(GameObjectType.Object12)
			};

			foreach (var option in ObjectTypeOptions)
			{
				option.PropertyChanged += OnTypeOptionPropertyChanged;
			}

			_allOffsetOptions = ActionOffsets.All
				.OrderBy(o => o.Category)
				.ThenBy(o => o.Label)
				.Select(o => new ActionOffsetOption(o))
				.ToList();

			ObjectsView = CollectionViewSource.GetDefaultView(AllObjects);
			ObjectsView.Filter = FilterPredicate;

			LoadObjectsCommand = new RelayCommand(_ => LoadObjects());
			SearchByNameCommand = new RelayCommand(_ => SearchByName(), _ => !string.IsNullOrWhiteSpace(FilterText));
			ClearCommand = new RelayCommand(_ =>
			{
				AllObjects.Clear();
				SelectedObject = null;
				ObjectCount = 0;
				StatusMessage = "Cleared.";
				OnPropertyChanged(nameof(HasObjects));
				NpcSearchResults.Clear();
				NpcLookupStatus = string.Empty;
				NpcSnapshotCount = 0;
				ObjectsView?.Refresh();
			});
			DoActionCommand = new RelayCommand(_ => DoAction(), _ => CanDoAction());
			ToggleSidePanelCommand = new RelayCommand(_ => IsSidePanelCollapsed = !IsSidePanelCollapsed);
			LookupNpcByIdCommand = new RelayCommand(
				_ => LookupNpcById(),
				_ =>
				{
					if (!IsNpcSelected)
						return false;

					var token = FilterText?.Trim();
					if (string.IsNullOrWhiteSpace(token))
						return false;

					return int.TryParse(token, out var _);
				});

			_updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			_updateTimer.Tick += OnTimerTick;

			StatusMessage = "Select a type and click Load.";
			UpdateObjectTypeVisibility();
			NpcSearchResults.CollectionChanged += OnNpcSearchResultsChanged;
			RefreshOffsetOptions();
		}

		private void UpdateObjectTypeVisibility()
		{
			var activeTypes = GetActiveTypes();
			IsNpcSelected = activeTypes.Contains(GameObjectType.NPC);
			IsObjectsSelected = activeTypes.Contains(GameObjectType.Object) || activeTypes.Contains(GameObjectType.Object12);
			RefreshOffsetOptions();
		}

		private void RefreshOffsetOptions()
		{
			var categories = GetActiveOffsetCategories();
			var filtered = _allOffsetOptions
				.Where(o => categories.Count == 0 || categories.Contains(o.Category))
				.ToList();

			OffsetOptions.Clear();
			foreach (var option in filtered)
			{
				OffsetOptions.Add(option);
			}

			if (OffsetOptions.Count == 0)
			{
				SelectedOffset = null;
			}
			else if (!OffsetOptions.Contains(SelectedOffset))
			{
				SelectedOffset = OffsetOptions[0];
			}
		}

		private HashSet<GameObjectType> GetActiveTypes()
		{
			var active = new HashSet<GameObjectType>();

			var allOption = ObjectTypeOptions.FirstOrDefault(o => o.Type == GameObjectType.All);
			var allSelected = allOption?.IsSelected == true;

			var explicitSelections = ObjectTypeOptions
				.Where(o => o.Type != GameObjectType.All && o.IsSelected)
				.Select(o => o.Type)
				.ToList();

			if (explicitSelections.Count == 0 || allSelected)
			{
				foreach (var option in ObjectTypeOptions.Where(o => o.Type != GameObjectType.All))
				{
					active.Add(option.Type);
				}
			}
			else
			{
				foreach (var type in explicitSelections)
				{
					active.Add(type);
				}
			}

			return active;
		}

		private HashSet<ActionOffsets.OffsetCategory> GetActiveOffsetCategories()
		{
			var categories = new HashSet<ActionOffsets.OffsetCategory>();
			var activeTypes = GetActiveTypes();

			if (activeTypes.Contains(GameObjectType.NPC))
				categories.Add(ActionOffsets.OffsetCategory.Npc);

			if (activeTypes.Contains(GameObjectType.Player))
				categories.Add(ActionOffsets.OffsetCategory.Player);

			if (activeTypes.Contains(GameObjectType.Object) || activeTypes.Contains(GameObjectType.Object12) ||
				activeTypes.Contains(GameObjectType.GroundItem) || activeTypes.Contains(GameObjectType.Highlight) ||
				activeTypes.Contains(GameObjectType.Projectile))
			{
				categories.Add(ActionOffsets.OffsetCategory.Object);
				categories.Add(ActionOffsets.OffsetCategory.Interface);
			}

			if (activeTypes.Contains(GameObjectType.Tile) || activeTypes.Contains(GameObjectType.Object) ||
				activeTypes.Contains(GameObjectType.Object12) || activeTypes.Contains(GameObjectType.NPC) ||
				activeTypes.Contains(GameObjectType.Player))
			{
				categories.Add(ActionOffsets.OffsetCategory.Movement);
			}

			return categories;
		}

		private bool MatchesSelectedTypes(Objects.GameObject obj, ICollection<GameObjectType> activeTypes)
		{
			if (activeTypes.Count == 0)
				return true;

			foreach (var type in activeTypes)
			{
				if (MatchesType(obj, type))
					return true;
			}
			return false;
		}

		private static bool MatchesType(Objects.GameObject obj, GameObjectType type) => type switch
		{
			GameObjectType.Object => obj.Type == (int)Objects.ObjectKind.Object,
			GameObjectType.NPC => obj.Type == (int)Objects.ObjectKind.Npc,
			GameObjectType.Player => obj.Type == (int)Objects.ObjectKind.Player,
			GameObjectType.GroundItem => obj.Type == (int)Objects.ObjectKind.GroundItem,
			GameObjectType.Highlight => obj.Type == (int)Objects.ObjectKind.Highlight,
			GameObjectType.Projectile => obj.Type == (int)Objects.ObjectKind.Projectile,
			GameObjectType.Tile => obj.Type == (int)Objects.ObjectKind.Tile,
			GameObjectType.Object12 => obj.Type == (int)Objects.ObjectKind.Object12,
			_ => true
		};

		private string DescribeTypes(ICollection<GameObjectType> types)
		{
			if (types.Count == 0)
				return "all types";

			var labels = types
				.Select(t => ObjectTypeOptions.FirstOrDefault(o => o.Type == t)?.DisplayName ?? t.ToString());

			return string.Join(", ", labels);
		}

		private void PopulateObjects(IEnumerable<Objects.GameObject> objects)
		{
			AllObjects.Clear();
			SelectedObject = null;

			foreach (var obj in objects)
			{
				AllObjects.Add(obj);
			}

			ObjectCount = AllObjects.Count;
			OnPropertyChanged(nameof(HasObjects));
			ObjectsView?.Refresh();
		}

		private void OnTypeOptionPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(GameObjectTypeOption.IsSelected) || _suppressTypeOptionNotifications)
				return;

			if (sender is not GameObjectTypeOption option)
				return;

			if (option.Type == GameObjectType.All)
			{
				try
				{
					_suppressTypeOptionNotifications = true;
					foreach (var other in ObjectTypeOptions.Where(o => o.Type != GameObjectType.All))
					{
						other.IsSelected = option.IsSelected;
					}
				}
				finally
				{
					_suppressTypeOptionNotifications = false;
				}
			}
			else if (option.IsSelected && ObjectTypeOptions.FirstOrDefault(o => o.Type == GameObjectType.All)?.IsSelected == true)
			{
				try
				{
					_suppressTypeOptionNotifications = true;
					var allOption = ObjectTypeOptions.First(o => o.Type == GameObjectType.All);
					allOption.IsSelected = false;
				}
				finally
				{
					_suppressTypeOptionNotifications = false;
				}
			}

			UpdateObjectTypeVisibility();
			ObjectsView?.Refresh();
		}

		private void SearchByName()
		{
			try
			{
				var token = FilterText?.Trim();
				if (string.IsNullOrWhiteSpace(token))
				{
					StatusMessage = "Enter a name to search.";
					return;
				}

				var activeTypes = GetActiveTypes();
				var results = Objects.ByName(token)
					.Where(o => MatchesSelectedTypes(o, activeTypes))
					.ToList();

				PopulateObjects(results);
				StatusMessage = results.Count > 0
					? $"Search \"{token}\" returned {results.Count} object(s)."
					: $"No objects found for \"{token}\".";

				if (IsNpcSelected)
				{
					var npcResults = Npcs.ByName(token);
					NpcSearchResults.Clear();
					foreach (var npc in npcResults)
					{
						NpcSearchResults.Add(npc);
					}
					NpcLookupStatus = npcResults.Count > 0
						? $"NPC search found {npcResults.Count} result(s)."
						: $"No NPCs named \"{token}\".";
				}
				else if (NpcSearchResults.Count > 0)
				{
					NpcSearchResults.Clear();
					NpcLookupStatus = string.Empty;
				}
			}
			catch (Exception ex)
			{
				StatusMessage = $"Search error: {ex.Message}";
			}
		}

		private void OnNpcSearchResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
			=> OnPropertyChanged(nameof(HasNpcSearchResults));

		private void LookupNpcById()
		{
			try
			{
				if (!int.TryParse(FilterText?.Trim(), out var id))
				{
					StatusMessage = "Enter a numeric NPC id to look up.";
					return;
				}

				var npc = Npcs.GetById(id);
				if (npc != null)
				{
					NpcLookupStatus = $"NPC {npc.Name} (ID {npc.Id}) at ({npc.X}, {npc.Y}) with {npc.Health} HP.";
					SelectedNpcSearchResult = npc;
				}
				else
				{
					NpcLookupStatus = $"NPC with id {id} not found.";
				}
			}
			catch (Exception ex)
			{
				StatusMessage = $"Lookup error: {ex.Message}";
			}
		}

		private void LoadObjects()
		{
			try
			{
				var activeTypes = GetActiveTypes();
				var allObjects = Objects.GetAll();
				var filtered = allObjects.Where(obj => MatchesSelectedTypes(obj, activeTypes)).ToList();

				PopulateObjects(filtered);

				if (activeTypes.Contains(GameObjectType.NPC))
				{
					var npcSnapshot = Npcs.GetAll();
					NpcSnapshotCount = npcSnapshot.Count;
				}
				else
				{
					NpcSnapshotCount = 0;
				}

				StatusMessage = $"Loaded {ObjectCount} object(s) for {DescribeTypes(activeTypes)}.";
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

			var activeTypes = GetActiveTypes();
			if (!MatchesSelectedTypes(go, activeTypes))
				return false;

			// NPC-specific filters
			if (ShowOnlyAlive && go.Type == (int)Objects.ObjectKind.Npc && go.Life <= 0)
				return false;

			// Objects-specific filters
			if (OnlyInteractable &&
				(go.Type == (int)Objects.ObjectKind.Object || go.Type == (int)Objects.ObjectKind.Object12) &&
				string.IsNullOrWhiteSpace(go.Action))
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
				bool npcResult = false;
				bool objectResult = false;
				var token = FilterText?.Trim() ?? string.Empty;
				var activeTypes = GetActiveTypes();
				var shouldTryNpc = activeTypes.Count == 0 || activeTypes.Contains(GameObjectType.NPC) ||
					(SelectedObject?.Type == (int)Objects.ObjectKind.Npc);
				var shouldTryObjects = activeTypes.Count == 0 ||
					activeTypes.Any(t => t != GameObjectType.NPC) ||
					(SelectedObject != null && SelectedObject.Type != (int)Objects.ObjectKind.Npc);
				var offset = SelectedOffset?.Value ?? 0;

				if (!string.IsNullOrWhiteSpace(token))
				{
					if (int.TryParse(token, out var id))
					{
						if (shouldTryNpc)
							npcResult |= Npcs.DoActionByIds(new[] { id }, SelectedActionIndex, offset);

						if (shouldTryObjects)
							objectResult |= Objects.DoActionByIds(new[] { id }, SelectedActionIndex, offset);
					}
					else
					{
						if (shouldTryNpc)
							npcResult |= Npcs.DoActionByNames(new[] { token }, SelectedActionIndex, offset);

						if (shouldTryObjects)
							objectResult |= Objects.DoActionByNames(new[] { token }, SelectedActionIndex, offset);
					}
				}
				else if (SelectedObject != null)
				{
					if (SelectedObject.Type == (int)Objects.ObjectKind.Npc)
					{
						npcResult = Npcs.DoActionByNames(new[] { SelectedObject.Name }, SelectedActionIndex, offset);
					}

					objectResult = SelectedObject.DoAction(SelectedActionIndex, offset);
				}
				else
				{
					StatusMessage = "Select an object or provide a filter before executing an action.";
					return;
				}

				var ok = npcResult || objectResult;

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

			try
			{
				_updateTimer.Tick -= OnTimerTick;
			}
			catch { /* ignore */ }

			foreach (var option in ObjectTypeOptions)
			{
				option.PropertyChanged -= OnTypeOptionPropertyChanged;
			}

			NpcSearchResults.CollectionChanged -= OnNpcSearchResultsChanged;

			_disposed = true;
		}
		#endregion
	}
}
