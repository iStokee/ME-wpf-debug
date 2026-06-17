using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public sealed class KnowledgeViewModel : BaseViewModel, IActivatableViewModel
    {
        private KnowledgeItem? _selectedItem;
        private string _searchText = string.Empty;
        private string _categoryFilter = string.Empty;
        private string _statusFilter = string.Empty;
        private string _kindFilter = string.Empty;
        private bool _blockingFilter = false;
        private bool _questionDebtFilter = false;
        private string _targetFilter = string.Empty;
        private string _timeHorizonFilter = string.Empty;
        private string _viewPreset = "All";
        private string _lastStatus = "Ready.";

        // Core editor fields
        private string _title = string.Empty;
        private string _body = string.Empty;
        private string _kind = "fact";
        private string _category = "general";
        private string _tags = string.Empty;
        private string _status = "draft";
        private string _source = "human";
        private int _confidence = 50;
        private string _relatedType = string.Empty;
        private string _relatedId = string.Empty;

        // Extended editor fields
        private string _target = string.Empty;
        private string _timeHorizon = string.Empty;
        private string _mode = string.Empty;
        private int _impact = 3;
        private int _urgency = 3;
        private bool _isBlocking = false;
        private string _deferReason = string.Empty;
        private string _evidenceSummary = string.Empty;
        private int _validationCount;
        private int _failureCount;
        private int _usageCount;
        private string _lastReviewedAt = string.Empty;
        private string _lastValidatedAt = string.Empty;
        private string _originatingAgent = string.Empty;
        private string _originatingSession = string.Empty;
        private string _originatingTask = string.Empty;
        private string _relatedItemIdsText = string.Empty;
        private string _createdUtc = string.Empty;
        private string _updatedUtc = string.Empty;

        // Filter collections
        public ObservableCollection<KnowledgeItem> Items { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Statuses { get; } = new();
        public ObservableCollection<string> Kinds { get; } = new();
        public ObservableCollection<string> Targets { get; } = new();
        public ObservableCollection<string> TimeHorizons { get; } = new();
        public ObservableCollection<string> ViewPresets { get; } = new(new[]
        {
            "All", "Inquiry Ledger", "Question Debt", "Waiting on User",
            "Validated Knowledge", "Stale Knowledge", "Recent Outcomes"
        });

        public KnowledgeItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    LoadEditor(value);
            }
        }

        // Filter properties
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) RefreshItems(); } }
        public string CategoryFilter { get => _categoryFilter; set { if (SetProperty(ref _categoryFilter, value)) RefreshItems(); } }
        public string StatusFilter { get => _statusFilter; set { if (SetProperty(ref _statusFilter, value)) RefreshItems(); } }
        public string KindFilter { get => _kindFilter; set { if (SetProperty(ref _kindFilter, value)) RefreshItems(); } }
        public bool BlockingFilter { get => _blockingFilter; set { if (SetProperty(ref _blockingFilter, value)) RefreshItems(); } }
        public bool QuestionDebtFilter { get => _questionDebtFilter; set { if (SetProperty(ref _questionDebtFilter, value)) RefreshItems(); } }
        public string TargetFilter { get => _targetFilter; set { if (SetProperty(ref _targetFilter, value)) RefreshItems(); } }
        public string TimeHorizonFilter { get => _timeHorizonFilter; set { if (SetProperty(ref _timeHorizonFilter, value)) RefreshItems(); } }
        public string ViewPreset
        {
            get => _viewPreset;
            set
            {
                if (SetProperty(ref _viewPreset, value))
                    ApplyPreset(value);
            }
        }

        public string LastStatus { get => _lastStatus; set => SetProperty(ref _lastStatus, value); }

        // Core editor properties
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Body { get => _body; set => SetProperty(ref _body, value); }
        public string Kind { get => _kind; set => SetProperty(ref _kind, value); }
        public string Category { get => _category; set => SetProperty(ref _category, value); }
        public string Tags { get => _tags; set => SetProperty(ref _tags, value); }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        public int Confidence { get => _confidence; set => SetProperty(ref _confidence, Math.Clamp(value, 0, 100)); }
        public string RelatedType { get => _relatedType; set => SetProperty(ref _relatedType, value); }
        public string RelatedId { get => _relatedId; set => SetProperty(ref _relatedId, value); }
        public string StorePath => Knowledge.GetStorePath();

        // Extended editor properties
        public string Target { get => _target; set => SetProperty(ref _target, value); }
        public string TimeHorizon { get => _timeHorizon; set => SetProperty(ref _timeHorizon, value); }
        public string ItemMode { get => _mode; set => SetProperty(ref _mode, value); }
        public int Impact { get => _impact; set => SetProperty(ref _impact, Math.Clamp(value, 1, 5)); }
        public int Urgency { get => _urgency; set => SetProperty(ref _urgency, Math.Clamp(value, 1, 5)); }
        public bool IsBlocking { get => _isBlocking; set => SetProperty(ref _isBlocking, value); }
        public string DeferReason { get => _deferReason; set => SetProperty(ref _deferReason, value); }
        public string EvidenceSummary { get => _evidenceSummary; set => SetProperty(ref _evidenceSummary, value); }
        public int ValidationCount { get => _validationCount; set => SetProperty(ref _validationCount, value); }
        public int FailureCount { get => _failureCount; set => SetProperty(ref _failureCount, value); }
        public int UsageCount { get => _usageCount; set => SetProperty(ref _usageCount, value); }
        public string LastReviewedAt { get => _lastReviewedAt; set => SetProperty(ref _lastReviewedAt, value); }
        public string LastValidatedAt { get => _lastValidatedAt; set => SetProperty(ref _lastValidatedAt, value); }
        public string OriginatingAgent { get => _originatingAgent; set => SetProperty(ref _originatingAgent, value); }
        public string OriginatingSession { get => _originatingSession; set => SetProperty(ref _originatingSession, value); }
        public string OriginatingTask { get => _originatingTask; set => SetProperty(ref _originatingTask, value); }
        public string RelatedItemIdsText { get => _relatedItemIdsText; set => SetProperty(ref _relatedItemIdsText, value); }
        public string CreatedUtc { get => _createdUtc; set => SetProperty(ref _createdUtc, value); }
        public string UpdatedUtc { get => _updatedUtc; set => SetProperty(ref _updatedUtc, value); }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ArchiveCommand { get; }
        public ICommand PromoteTrustedCommand { get; }
        public ICommand MarkDeprecatedCommand { get; }
        public ICommand ReviewCommand { get; }

        public KnowledgeViewModel()
        {
            RefreshCommand = new RelayCommand(_ => RefreshItems());
            NewCommand = new RelayCommand(_ => NewItem());
            SaveCommand = new RelayCommand(_ => SaveItem(), _ => !string.IsNullOrWhiteSpace(Title));
            DeleteCommand = new RelayCommand(_ => DeleteItem(), _ => SelectedItem != null);
            ArchiveCommand = new RelayCommand(_ => ArchiveItem(), _ => SelectedItem != null);
            PromoteTrustedCommand = new RelayCommand(_ => PromoteItem(), _ => SelectedItem != null);
            MarkDeprecatedCommand = new RelayCommand(_ => { Status = "deprecated"; SaveItem(); }, _ => SelectedItem != null);
            ReviewCommand = new RelayCommand(_ => ReviewItem(), _ => SelectedItem != null);

            RefreshItems();
            NewItem();
        }

        public void OnActivated() => RefreshItems();
        public void OnDeactivated() { }

        private void RefreshItems()
        {
            var previousId = SelectedItem?.Id;
            var items = Knowledge.List(new KnowledgeQuery
            {
                Search = SearchText,
                Category = CategoryFilter,
                Status = StatusFilter,
                Kind = KindFilter,
                Blocking = BlockingFilter ? true : (bool?)null,
                Target = string.IsNullOrWhiteSpace(TargetFilter) ? null : TargetFilter,
                TimeHorizon = string.IsNullOrWhiteSpace(TimeHorizonFilter) ? null : TimeHorizonFilter,
                MaxCount = 500
            }).ToList();

            if (QuestionDebtFilter)
                items = items.Where(IsQuestionDebt).ToList();

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

            RefreshFacets(items);
            if (!string.IsNullOrWhiteSpace(previousId))
                SelectedItem = Items.FirstOrDefault(i => string.Equals(i.Id, previousId, StringComparison.OrdinalIgnoreCase));

            LastStatus = $"{Items.Count} item(s) · {StorePath}";
        }

        private static bool IsQuestionDebt(KnowledgeItem item)
        {
            var debtStatuses = new[] { "open", "investigating", "partial", "waiting-user" };
            if ((string.Equals(item.Kind, "inquiry", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(item.Kind, "hypothesis", StringComparison.OrdinalIgnoreCase)) &&
                debtStatuses.Any(s => string.Equals(item.Status, s, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (string.Equals(item.Kind, "answer", StringComparison.OrdinalIgnoreCase) && item.ValidationCount == 0)
                return true;

            if (string.Equals(item.Kind, "knowledge", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Status, "stale", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void ApplyPreset(string preset)
        {
            // Reset filters first, then apply preset
            _blockingFilter = false;
            _questionDebtFilter = false;
            _targetFilter = string.Empty;
            _timeHorizonFilter = string.Empty;

            switch (preset)
            {
                case "Inquiry Ledger":
                    _kindFilter = "inquiry";
                    _statusFilter = string.Empty;
                    _categoryFilter = string.Empty;
                    break;
                case "Question Debt":
                    _questionDebtFilter = true;
                    _kindFilter = string.Empty;
                    _statusFilter = string.Empty;
                    _categoryFilter = string.Empty;
                    break;
                case "Waiting on User":
                    _statusFilter = "waiting-user";
                    _kindFilter = string.Empty;
                    _categoryFilter = string.Empty;
                    break;
                case "Validated Knowledge":
                    _kindFilter = "knowledge";
                    _statusFilter = "validated";
                    _categoryFilter = string.Empty;
                    break;
                case "Stale Knowledge":
                    _statusFilter = "stale";
                    _kindFilter = string.Empty;
                    _categoryFilter = string.Empty;
                    break;
                case "Recent Outcomes":
                    _kindFilter = "outcome";
                    _statusFilter = string.Empty;
                    _categoryFilter = string.Empty;
                    break;
                default: // "All"
                    _kindFilter = string.Empty;
                    _statusFilter = string.Empty;
                    _categoryFilter = string.Empty;
                    break;
            }

            RaisePropertyChanged(nameof(KindFilter));
            RaisePropertyChanged(nameof(StatusFilter));
            RaisePropertyChanged(nameof(CategoryFilter));
            RaisePropertyChanged(nameof(BlockingFilter));
            RaisePropertyChanged(nameof(QuestionDebtFilter));
            RaisePropertyChanged(nameof(TargetFilter));
            RaisePropertyChanged(nameof(TimeHorizonFilter));
            RefreshItems();
        }

        private void RefreshFacets(IReadOnlyList<KnowledgeItem> items)
        {
            RefreshFacet(Categories, items.Select(i => i.Category), CategoryFilter);
            RefreshFacet(Statuses, items.Select(i => i.Status), StatusFilter);
            RefreshFacet(Kinds, items.Select(i => i.Kind), KindFilter);
            RefreshFacet(Targets, items.Select(i => i.Target).Where(t => !string.IsNullOrWhiteSpace(t)), TargetFilter);
            RefreshFacet(TimeHorizons, items.Select(i => i.TimeHorizon).Where(t => !string.IsNullOrWhiteSpace(t)), TimeHorizonFilter);
        }

        private static void RefreshFacet(ObservableCollection<string> target, IEnumerable<string> values, string selected)
        {
            var next = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Prepend(string.Empty)
                .ToList();

            if (!string.IsNullOrWhiteSpace(selected) && !next.Contains(selected, StringComparer.OrdinalIgnoreCase))
                next.Add(selected);

            target.Clear();
            foreach (var value in next)
                target.Add(value);
        }

        private void NewItem()
        {
            SelectedItem = null;
            Title = "New knowledge item";
            Body = string.Empty;
            Kind = "fact";
            Category = string.IsNullOrWhiteSpace(CategoryFilter) ? "general" : CategoryFilter;
            Tags = string.Empty;
            Status = "draft";
            Source = "human";
            Confidence = 50;
            RelatedType = string.Empty;
            RelatedId = string.Empty;
            Target = string.Empty;
            TimeHorizon = string.Empty;
            ItemMode = string.Empty;
            Impact = 3;
            Urgency = 3;
            IsBlocking = false;
            DeferReason = string.Empty;
            EvidenceSummary = string.Empty;
            ValidationCount = 0;
            FailureCount = 0;
            UsageCount = 0;
            LastReviewedAt = string.Empty;
            LastValidatedAt = string.Empty;
            OriginatingAgent = string.Empty;
            OriginatingSession = string.Empty;
            OriginatingTask = string.Empty;
            RelatedItemIdsText = string.Empty;
            CreatedUtc = string.Empty;
            UpdatedUtc = string.Empty;
            LastStatus = "Editing new knowledge item.";
        }

        private void LoadEditor(KnowledgeItem? item)
        {
            if (item == null)
                return;

            Title = item.Title;
            Body = item.Body;
            Kind = item.Kind;
            Category = item.Category;
            Tags = string.Join(", ", item.Tags);
            Status = item.Status;
            Source = item.Source;
            Confidence = item.Confidence;
            RelatedType = item.RelatedType ?? string.Empty;
            RelatedId = item.RelatedId ?? string.Empty;

            Target = item.Target;
            TimeHorizon = item.TimeHorizon;
            ItemMode = item.Mode;
            Impact = item.Impact;
            Urgency = item.Urgency;
            IsBlocking = item.Blocking;
            DeferReason = item.DeferReason;
            EvidenceSummary = item.EvidenceSummary;
            ValidationCount = item.ValidationCount;
            FailureCount = item.FailureCount;
            UsageCount = item.UsageCount;
            LastReviewedAt = item.LastReviewedAt.HasValue ? item.LastReviewedAt.Value.ToString("yyyy-MM-dd HH:mm UTC") : string.Empty;
            LastValidatedAt = item.LastValidatedAt.HasValue ? item.LastValidatedAt.Value.ToString("yyyy-MM-dd HH:mm UTC") : string.Empty;
            OriginatingAgent = item.OriginatingAgent;
            OriginatingSession = item.OriginatingSession;
            OriginatingTask = item.OriginatingTask;
            RelatedItemIdsText = string.Join(", ", item.RelatedItemIds);
            CreatedUtc = item.CreatedUtc.ToString("yyyy-MM-dd HH:mm UTC");
            UpdatedUtc = item.UpdatedUtc.ToString("yyyy-MM-dd HH:mm UTC");
        }

        private void SaveItem()
        {
            try
            {
                var item = SelectedItem ?? new KnowledgeItem();
                item.Title = Title;
                item.Body = Body;
                item.Kind = Kind;
                item.Category = Category;
                item.Tags = SplitTags(Tags);
                item.Status = Status;
                item.Source = Source;
                item.Confidence = Confidence;
                item.RelatedType = RelatedType;
                item.RelatedId = RelatedId;
                item.Target = Target;
                item.TimeHorizon = TimeHorizon;
                item.Mode = ItemMode;
                item.Impact = Impact;
                item.Urgency = Urgency;
                item.Blocking = IsBlocking;
                item.DeferReason = DeferReason;
                item.EvidenceSummary = EvidenceSummary;
                item.OriginatingAgent = OriginatingAgent;
                item.OriginatingSession = OriginatingSession;
                item.OriginatingTask = OriginatingTask;
                item.RelatedItemIds = SplitTags(RelatedItemIdsText);

                var saved = Knowledge.Save(item);
                LastStatus = $"Saved '{saved.Title}'.";
                RefreshItems();
                SelectedItem = Items.FirstOrDefault(i => string.Equals(i.Id, saved.Id, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                LastStatus = $"Save failed: {ex.Message}";
            }
        }

        private void DeleteItem()
        {
            if (SelectedItem == null)
                return;

            var title = SelectedItem.Title;
            if (Knowledge.Delete(SelectedItem.Id))
            {
                LastStatus = $"Deleted '{title}'.";
                RefreshItems();
                NewItem();
            }
            else
            {
                LastStatus = "Delete failed: item was not found.";
            }
        }

        private void ArchiveItem()
        {
            if (SelectedItem == null)
                return;

            Status = "archived";
            SaveItem();
        }

        private void PromoteItem()
        {
            if (SelectedItem == null)
                return;

            try
            {
                var promoted = Knowledge.Promote(SelectedItem.Id, Title, Body, Math.Max(Confidence, 80));
                LastStatus = $"Promoted '{SelectedItem.Title}' to reusable knowledge.";
                RefreshItems();
                SelectedItem = Items.FirstOrDefault(i => string.Equals(i.Id, promoted.Id, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                LastStatus = $"Promote failed: {ex.Message}";
            }
        }

        private void ReviewItem()
        {
            if (SelectedItem == null)
                return;

            var item = SelectedItem;
            item.LastReviewedAt = DateTime.UtcNow;
            item.UpdatedUtc = DateTime.UtcNow;

            try
            {
                var saved = Knowledge.Save(item);
                LastStatus = $"Reviewed '{saved.Title}' at {DateTime.UtcNow:HH:mm UTC}.";
                RefreshItems();
                SelectedItem = Items.FirstOrDefault(i => string.Equals(i.Id, saved.Id, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                LastStatus = $"Review failed: {ex.Message}";
            }
        }

        private static List<string> SplitTags(string text)
            => (text ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
    }
}
