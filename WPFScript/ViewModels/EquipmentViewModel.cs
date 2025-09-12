using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public class EquipmentViewModel : INotifyPropertyChanged
    {
        private bool _isOpen, _isEmpty, _isFull;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }
        public bool IsEmpty { get => _isEmpty; set => SetProperty(ref _isEmpty, value); }
        public bool IsFull { get => _isFull; set => SetProperty(ref _isFull, value); }

        public ObservableCollection<Equipment.Item> Items { get; } = new();

        public ICommand RefreshStateCommand { get; }
        public ICommand LoadItemsCommand { get; }
        public ICommand ContainsAnyCommand { get; }
        public ICommand ContainsAllCommand { get; }
        public ICommand ContainsOnlyCommand { get; }
        public ICommand UnequipByIdCommand { get; }
        public ICommand UnequipByNameCommand { get; }
        public ICommand AutoUnequipCommand { get; }

        // Quick filter (id(s) or name) with live indicators
        private string _quickFilterText;
        //private int _quickCount;
        public string QuickFilterText { get => _quickFilterText; set { if (SetProperty(ref _quickFilterText, value)) UpdateQuickFilter(); } }
        //public int QuickCount { get => _quickCount; set => SetProperty(ref _quickCount, value); }
        public ObservableCollection<Equipment.Item> QuickItems { get; } = new();

        private string _idsInput, _nameInput, _idInput;
        public string IdsInput { get => _idsInput; set => SetProperty(ref _idsInput, value); }
        public string NameInput { get => _nameInput; set => SetProperty(ref _nameInput, value); }
        public string IdInput { get => _idInput; set => SetProperty(ref _idInput, value); }

        private string _containsAnyResult, _containsAllResult, _containsOnlyResult, _actionResult;
        public string ContainsAnyResult { get => _containsAnyResult; set => SetProperty(ref _containsAnyResult, value); }
        public string ContainsAllResult { get => _containsAllResult; set => SetProperty(ref _containsAllResult, value); }
        public string ContainsOnlyResult { get => _containsOnlyResult; set => SetProperty(ref _containsOnlyResult, value); }
        public string ActionResult { get => _actionResult; set => SetProperty(ref _actionResult, value); }

        // Status flags for dots
        private bool _containsAnyFlag, _containsAllFlag, _containsOnlyFlag;
        private int _quickCount;
        public bool ContainsAnyFlag { get => _containsAnyFlag; set => SetProperty(ref _containsAnyFlag, value); }
        public bool ContainsAllFlag { get => _containsAllFlag; set => SetProperty(ref _containsAllFlag, value); }
        public bool ContainsOnlyFlag { get => _containsOnlyFlag; set => SetProperty(ref _containsOnlyFlag, value); }
        public int QuickCount { get => _quickCount; set => SetProperty(ref _quickCount, value); }

        public EquipmentViewModel()
        {
            RefreshStateCommand = new RelayCommand(_ =>
            {
                IsOpen = Equipment.IsOpen();
                IsEmpty = Equipment.IsEmpty();
                IsFull = Equipment.IsFull();
            });

            LoadItemsCommand = new RelayCommand(_ =>
            {
                Items.Clear();
                foreach (var it in Equipment.GetAllItems()) Items.Add(it);
            });

            ContainsAnyCommand = new RelayCommand(_ =>
            {
                var ids = ParseIds(IdsInput);
                var any = Equipment.ContainsAny(ids);
                ContainsAnyFlag = any; ContainsAnyResult = any ? "True" : "False";
            });
            ContainsAllCommand = new RelayCommand(_ =>
            {
                var ids = ParseIds(IdsInput);
                var all = Equipment.ContainsAll(ids);
                ContainsAllFlag = all; ContainsAllResult = all ? "True" : "False";
            });
            ContainsOnlyCommand = new RelayCommand(_ =>
            {
                var ids = ParseIds(IdsInput);
                var only = Equipment.ContainsOnly(ids);
                ContainsOnlyFlag = only; ContainsOnlyResult = only ? "True" : "False";
            });

            UnequipByIdCommand = new RelayCommand(_ =>
            {
                if (int.TryParse(IdInput, out var id))
                    ActionResult = Equipment.UnequipById(id) ? "✔ Unequipped" : "✘ Failed";
            });
            UnequipByNameCommand = new RelayCommand(_ =>
            {
                var name = NameInput;
                if (!string.IsNullOrWhiteSpace(name))
                    ActionResult = Equipment.UnequipByName(name) ? "✔ Unequipped" : "✘ Failed";
            });

            AutoUnequipCommand = new RelayCommand(_ =>
            {
                if (int.TryParse(IdInput, out var id))
                    ActionResult = Equipment.UnequipById(id) ? "✔ Unequipped" : "✘ Failed";
                else if (!string.IsNullOrWhiteSpace(NameInput))
                    ActionResult = Equipment.UnequipByName(NameInput) ? "✔ Unequipped" : "✘ Failed";
            });

            UpdateQuickFilter();
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

        private void UpdateQuickFilter()
        {
            QuickItems.Clear();
            QuickCount = 0;

            var q = QuickFilterText;
            if (string.IsNullOrWhiteSpace(q)) { return; }

            // If looks like IDs (number or comma/space separated numbers)
            var looksLikeIds = false;
            var ids = Array.Empty<int>();
            if (int.TryParse(q.Trim(), out var singleId))
            {
                looksLikeIds = true;
                ids = new[] { singleId };
            }
            else if (q.IndexOf(',') >= 0 || q.IndexOf(' ') >= 0)
            {
                ids = ParseIds(q);
                looksLikeIds = ids.Length > 0;
            }

            if (looksLikeIds)
            {
                IdsInput = string.Join(", ", ids);
                // Update contains results to mirror quick filter
                var any = Equipment.ContainsAny(ids);
                var all = Equipment.ContainsAll(ids);
                var only = Equipment.ContainsOnly(ids);
                ContainsAnyFlag = any; ContainsAnyResult = any ? "True" : "False";
                ContainsAllFlag = all; ContainsAllResult = all ? "True" : "False";
                ContainsOnlyFlag = only; ContainsOnlyResult = only ? "True" : "False";

                // Show matching items (by id) for context
                foreach (var it in Equipment.GetAllItems())
                    if (Array.IndexOf(ids, it.Id) >= 0) QuickItems.Add(it);
                QuickCount = QuickItems.Count;

                if (ids.Length == 1) IdInput = ids[0].ToString();
            }
            else
            {
                NameInput = q;
                foreach (var it in Equipment.GetAllItems())
                    if (!string.IsNullOrEmpty(it.Name) && it.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        QuickItems.Add(it);
                QuickCount = QuickItems.Count;
            }
        }
    }
}
