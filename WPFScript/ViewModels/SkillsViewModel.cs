using MESharp.API;
using MESharp.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Threading;

namespace MESharp.ViewModels
{
    internal class SkillsViewModel : INotifyPropertyChanged
    {
        private enum SkillsDisplayMode
        {
            List,
            Grid,
            Table
        }

        private readonly SkillSession _session;
        private readonly DispatcherTimer _updateTimer;

        public ObservableCollection<SkillModel> AllSkills { get; }
        public ICollectionView SkillsView { get; }

        public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string>
        {
            "Skill Name",
            "Level",
            "Total XP",
            "XP Gained",
            "XP / Hour",
            "XP To Next",
            "ETA",
            "Active First"
        };

        private SkillsDisplayMode _displayMode = SkillsDisplayMode.List;

        public bool IsListView
        {
            get => _displayMode == SkillsDisplayMode.List;
            set
            {
                if (value)
                {
                    SetDisplayMode(SkillsDisplayMode.List);
                }
            }
        }

        public bool IsGridView
        {
            get => _displayMode == SkillsDisplayMode.Grid;
            set
            {
                if (value)
                {
                    SetDisplayMode(SkillsDisplayMode.Grid);
                }
            }
        }

        public bool IsTableView
        {
            get => _displayMode == SkillsDisplayMode.Table;
            set
            {
                if (value)
                {
                    SetDisplayMode(SkillsDisplayMode.Table);
                }
            }
        }

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

        private string _selectedSortOption = "XP Gained";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption == value) return;
                _selectedSortOption = value;
                OnPropertyChanged();
                ApplySorting();
            }
        }

        private bool _sortDescending = true;
        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (_sortDescending == value) return;
                _sortDescending = value;
                OnPropertyChanged();
                ApplySorting();
            }
        }

        public SkillsViewModel()
        {
            _session = new SkillSession();

            AllSkills = new ObservableCollection<SkillModel>(
                Enum.GetValues(typeof(SkillName))
                    .Cast<SkillName>()
                    .Select(name => new SkillModel(name, _session))
            );

            SkillsView = CollectionViewSource.GetDefaultView(AllSkills);
            SkillsView.Filter = o =>
            {
                var vm = (SkillModel)o;
                return !ShowOnlyActive || vm.XpGained > 0;
            };

            ApplySorting();

            _updateTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Background,
                (_, _) => RefreshAll(),
                Dispatcher.CurrentDispatcher
            );
            _updateTimer.Start();
        }

        private void RefreshAll()
        {
            foreach (var vm in AllSkills)
            {
                vm.Update();
            }

            SkillsView.Refresh();
        }

        private void ApplySorting()
        {
            ApplySorting(SelectedSortOption, SortDescending);
        }

        private void ApplySorting(string sortOption, bool sortDescending)
        {
            SkillsView.SortDescriptions.Clear();

            var direction = sortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            var propertyName = sortOption switch
            {
                "Skill Name" => nameof(SkillModel.Name),
                "Level" => nameof(SkillModel.LevelValue),
                "Total XP" => nameof(SkillModel.Xp),
                "XP Gained" => nameof(SkillModel.XpGained),
                "XP / Hour" => nameof(SkillModel.XpPerHour),
                "XP To Next" => nameof(SkillModel.XpToNext),
                "ETA" => nameof(SkillModel.EtaMinutes),
                "Active First" => nameof(SkillModel.IsActive),
                _ => nameof(SkillModel.XpGained)
            };

            SkillsView.SortDescriptions.Add(new SortDescription(propertyName, direction));

            if (propertyName != nameof(SkillModel.Name))
            {
                SkillsView.SortDescriptions.Add(new SortDescription(nameof(SkillModel.Name), ListSortDirection.Ascending));
            }

            SkillsView.Refresh();
        }

        public bool TrySetSortFromMember(string sortMemberPath, ListSortDirection? currentDirection)
        {
            if (string.IsNullOrWhiteSpace(sortMemberPath))
            {
                return false;
            }

            var option = sortMemberPath switch
            {
                nameof(SkillModel.Name) => "Skill Name",
                nameof(SkillModel.LevelValue) => "Level",
                nameof(SkillModel.Xp) => "Total XP",
                nameof(SkillModel.XpGained) => "XP Gained",
                nameof(SkillModel.XpPerHour) => "XP / Hour",
                nameof(SkillModel.XpToNext) => "XP To Next",
                nameof(SkillModel.EtaMinutes) => "ETA",
                nameof(SkillModel.IsActive) => "Active First",
                _ => null
            };

            if (option is null)
            {
                return false;
            }

            bool descending;
            if (SelectedSortOption == option)
            {
                descending = currentDirection != ListSortDirection.Descending;
            }
            else
            {
                descending = option != "Skill Name";
            }

            SetSort(option, descending);
            return true;
        }

        private void SetSort(string option, bool descending)
        {
            bool changed = false;
            if (_selectedSortOption != option)
            {
                _selectedSortOption = option;
                OnPropertyChanged(nameof(SelectedSortOption));
                changed = true;
            }

            if (_sortDescending != descending)
            {
                _sortDescending = descending;
                OnPropertyChanged(nameof(SortDescending));
                changed = true;
            }

            if (changed)
            {
                ApplySorting(_selectedSortOption, _sortDescending);
            }
        }

        private void SetDisplayMode(SkillsDisplayMode mode)
        {
            if (_displayMode == mode) return;
            _displayMode = mode;
            OnPropertyChanged(nameof(IsListView));
            OnPropertyChanged(nameof(IsGridView));
            OnPropertyChanged(nameof(IsTableView));
            SkillsView.Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
