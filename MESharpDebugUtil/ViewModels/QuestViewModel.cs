using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using MESharp.API;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class QuestVarbitChangeViewModel
    {
        public int VarbitId { get; init; }
        public int OldValue { get; init; }
        public int NewValue { get; init; }
    }

    /// <summary>
    /// Smoke-test surface for the Quest API: enumerate quests, read a quest's metadata + live progress,
    /// and run a <see cref="QuestWatcher"/> diff to confirm a varbit moved after an in-game step.
    /// </summary>
    public class QuestViewModel : INotifyPropertyChanged, IActivatableViewModel
    {
        private QuestWatcher? _watcher;

        private string _questName = string.Empty;
        public string QuestName
        {
            get => _questName;
            set { _questName = value; OnPropertyChanged(nameof(QuestName)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private string _details = string.Empty;
        public string Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(nameof(Details)); }
        }

        private int _questCount;
        public int QuestCount
        {
            get => _questCount;
            set { _questCount = value; OnPropertyChanged(nameof(QuestCount)); }
        }

        public ObservableCollection<string> Quests { get; } = new();
        public ObservableCollection<QuestVarbitChangeViewModel> Changes { get; } = new();

        private string? _selectedQuest;
        public string? SelectedQuest
        {
            get => _selectedQuest;
            set
            {
                _selectedQuest = value;
                OnPropertyChanged(nameof(SelectedQuest));
                if (!string.IsNullOrWhiteSpace(value))
                {
                    QuestName = value;
                }
            }
        }

        public ICommand LoadQuestsCommand { get; }
        public ICommand GetQuestCommand { get; }
        public ICommand WatchBeginCommand { get; }
        public ICommand WatchDiffCommand { get; }

        public QuestViewModel()
        {
            LoadQuestsCommand = new RelayCommand(_ => LoadQuests(), _ => Game.IsInjected);
            GetQuestCommand = new RelayCommand(_ => GetQuest(), _ => Game.IsInjected && !string.IsNullOrWhiteSpace(QuestName));
            WatchBeginCommand = new RelayCommand(_ => WatchBegin(), _ => Game.IsInjected && !string.IsNullOrWhiteSpace(QuestName));
            WatchDiffCommand = new RelayCommand(_ => WatchDiff(), _ => _watcher != null);
        }

        private void LoadQuests()
        {
            Quests.Clear();
            foreach (var name in Quest.Names)
            {
                Quests.Add(name);
            }
            QuestCount = Quest.Count;
            Status = $"Loaded {Quests.Count} quest name(s) from cache.";
        }

        private void GetQuest()
        {
            var info = Quest.Get(QuestName);
            if (info is null)
            {
                Details = string.Empty;
                Status = $"Quest '{QuestName}' not found.";
                return;
            }

            var reqSkills = info.RequiredSkills.Count == 0
                ? "(none)"
                : string.Join(", ", info.RequiredSkills.Select(s => $"skill {s.SkillId} >= {s.Level}"));
            var reqQuests = info.RequiredQuestIds.Count == 0
                ? "(none)"
                : string.Join(", ", info.RequiredQuestIds);

            Details =
                $"Name: {info.Name}  (cache id {info.Id}, list '{info.ListName}')\n" +
                $"Members: {info.Members}    Difficulty: {info.Difficulty}    Category: {info.Category}\n" +
                $"Quest points: reward {info.PointsReward}, required {info.PointsRequired}\n" +
                $"Progress varbit: {info.ProgressVarbit}  (start {info.ProgressStart} -> end {info.ProgressEnd})\n" +
                $"Live progress: {info.Progress}    Started: {info.IsStarted}    Complete: {info.IsComplete}\n" +
                $"Required quests: {reqQuests}\n" +
                $"Required skills: {reqSkills}\n" +
                $"Tracked varbits ({info.Varbits.Count}): {string.Join(", ", info.Varbits)}";

            Status = $"Loaded '{info.Name}' (progress {info.Progress}, complete={info.IsComplete}).";
        }

        private void WatchBegin()
        {
            _watcher = new QuestWatcher(QuestName);
            Changes.Clear();
            CommandManager.InvalidateRequerySuggested();
            Status = $"Baseline captured for '{QuestName}' over {_watcher.Varbits.Count} varbit(s). " +
                     "Perform a quest step in-game, then click 'Watch: Diff'.";
        }

        private void WatchDiff()
        {
            if (_watcher == null)
            {
                Status = "No active watcher. Click 'Watch: Begin' first.";
                return;
            }

            var changes = _watcher.DiffAndCommit();
            Changes.Clear();
            foreach (var change in changes)
            {
                Changes.Add(new QuestVarbitChangeViewModel
                {
                    VarbitId = change.VarbitId,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue
                });
            }

            Status = changes.Count == 0
                ? "No varbit changes since last snapshot."
                : $"{changes.Count} varbit(s) changed — step confirmed (re-baselined).";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void OnActivated()
            => Status = "Click 'Load Quests' to enumerate, then enter or pick a quest name.";

        public void OnDeactivated()
        {
        }
    }
}
