using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Threading;
using MESharp.API;

namespace MESharp.ViewModels
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private readonly SkillSession _session;
		private readonly DateTime _sessionStart;
		private readonly DispatcherTimer _updateTimer;

		private bool _isListView;
		public bool IsListView
		{
			get => _isListView;
			set
			{
				if (_isListView == value) return;
				_isListView = value;
				OnPropertyChanged();
				SkillsView.Refresh();
			}
		}

		public ObservableCollection<SkillViewModel> AllSkills { get; }
		public ICollectionView SkillsView { get; }

		private bool _showOnlyActive;
		public bool ShowOnlyActive
		{
			get => _showOnlyActive;
			set
			{
				if (_showOnlyActive == value) return;
				_showOnlyActive = value;
				OnPropertyChanged();
				SkillsView.Refresh();
			}
		}

		private string _sessionRuntimeString = "--:--:--";
		public string SessionRuntimeString
		{
			get => _sessionRuntimeString;
			private set
			{
				if (_sessionRuntimeString == value) return;
				_sessionRuntimeString = value;
				OnPropertyChanged();
			}
		}

		public MainWindowViewModel()
		{
			// remember when we started
			_sessionStart = DateTime.UtcNow;
			_session      = new SkillSession();

			// build our skill cards
			AllSkills = new ObservableCollection<SkillViewModel>(
				Enum.GetValues(typeof(SkillName))
					.Cast<SkillName>()
					.Select(name => new SkillViewModel(name, _session))
			);

			SkillsView = CollectionViewSource.GetDefaultView(AllSkills);
			SkillsView.Filter = o =>
			{
				var vm = (SkillViewModel)o;
				return !ShowOnlyActive || vm.XpGained > 0;
			};
			SkillsView.SortDescriptions.Add(
				new SortDescription(nameof(SkillViewModel.XpGained),
									ListSortDirection.Descending)
			);

			// timer to refresh XP *and* our runtime clock
			_updateTimer = new DispatcherTimer(
				TimeSpan.FromSeconds(1),
				DispatcherPriority.Background,
				(s, e) => RefreshAll(),
				Dispatcher.CurrentDispatcher
			);
			_updateTimer.Start();
		}

		private void RefreshAll()
		{
			foreach (var vm in AllSkills)
				vm.Update();

			// update the session‐timer string
			var elapsed = DateTime.UtcNow - _sessionStart;
			SessionRuntimeString = elapsed.ToString(@"hh\:mm\:ss");

			SkillsView.Refresh();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
	}
}

