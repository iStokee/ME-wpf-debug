using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
	public class NpcViewModel : INotifyPropertyChanged, IDisposable, IActivatableViewModel
	{
	private readonly DispatcherTimer _updateTimer;
    private bool _isActive;
	private bool _disposed;

	private bool _liveRefresh = true;
	public bool LiveRefresh
	{
		get => _liveRefresh;
		set
		{
			if (_liveRefresh == value) return;
			_liveRefresh = value;
			OnPropertyChanged();
			UpdateTimer();
		}
	}

	private void OnTimerTick(object? sender, EventArgs e)
	{
		if (_disposed) return;
		RefreshAll();
	}

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

		public IReadOnlyList<int> ActionIndices { get; } = Enumerable.Range(0, 11).ToArray();

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

		_updateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		_updateTimer.Tick += OnTimerTick;
		UpdateTimer();
	}

	private void UpdateTimer()
	{
		if (_disposed)
			return;

        if (!_isActive)
        {
            if (_updateTimer.IsEnabled)
            {
                _updateTimer.Stop();
            }
            return;
        }

		if (_liveRefresh)
		{
			if (!_updateTimer.IsEnabled)
			{
				_updateTimer.Start();
                RefreshAll();
			}
		}
		else if (_updateTimer.IsEnabled)
		{
			_updateTimer.Stop();
		}
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
		if (_disposed)
			return;

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

    public void OnActivated()
    {
        if (_disposed)
            return;

        if (_isActive)
        {
            RefreshAll();
            return;
        }

        _isActive = true;
        RefreshAll();
        UpdateTimer();
    }

    public void OnDeactivated()
    {
        if (_disposed || !_isActive)
            return;

        _isActive = false;
        try { _updateTimer.Stop(); } catch { /* ignore */ }
    }
	}
}
