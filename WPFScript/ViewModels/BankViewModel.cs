using MESharp.API;
using MESharp.Commands;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public class BankViewModel : INotifyPropertyChanged
    {
        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => Set(ref _isOpen, value); }

        // Quick filter (id or name) shows current stack size
        private string _quickFilterText;
        private int _quickStack;
        public string QuickFilterText { get => _quickFilterText; set { if (Set(ref _quickFilterText, value)) UpdateQuickFilter(); } }
        public int QuickStack { get => _quickStack; set { if (Set(ref _quickStack, value)) OnPropertyChanged(nameof(HasStack)); } }


		public bool HasStack => QuickStack > 0;

        private string _idsInput, _namesInput, _idInput, _nameInput;
        public string IdsInput { get => _idsInput; set => Set(ref _idsInput, value); }
        public string NamesInput { get => _namesInput; set => Set(ref _namesInput, value); }
        public string IdInput { get => _idInput; set => Set(ref _idInput, value); }
        public string NameInput { get => _nameInput; set => Set(ref _nameInput, value); }

        private string _actionResult, _stackResult;
        public string ActionResult { get => _actionResult; set => Set(ref _actionResult, value); }
        public string StackResult { get => _stackResult; set => Set(ref _stackResult, value); }

        public ICommand RefreshStateCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand DepositAllCommand { get; }
        public ICommand DepositAllExceptIdsCommand { get; }
        public ICommand DepositAllExceptNamesCommand { get; }
        public ICommand GetStackByIdCommand { get; }
        public ICommand GetStackByNameCommand { get; }
        public ICommand DoActionByIdCommand { get; }
        public ICommand DoActionByNameCommand { get; }

        private int _actionIndex, _offset;
        public int ActionIndex { get => _actionIndex; set => Set(ref _actionIndex, value); }
        public int Offset { get => _offset; set => Set(ref _offset, value); }

        public BankViewModel()
        {
            RefreshStateCommand = new RelayCommand(_ => IsOpen = Bank.IsOpen);
            CloseCommand = new RelayCommand(_ => { Bank.Close(); IsOpen = Bank.IsOpen; });
            DepositAllCommand = new RelayCommand(_ => ActionResult = Bank.DepositAll() ? "✔ Deposited" : "✘ Failed");
            DepositAllExceptIdsCommand = new RelayCommand(_ => {
                var ids = ParseIds(IdsInput);
                ActionResult = Bank.DepositAllExcept(ids) ? "✔ Deposited (except)" : "✘ Failed";
            });
            DepositAllExceptNamesCommand = new RelayCommand(_ => {
                var names = ParseNames(NamesInput);
                ActionResult = Bank.DepositAllExcept(names) ? "✔ Deposited (except)" : "✘ Failed";
            });
            GetStackByIdCommand = new RelayCommand(_ => {
                if (int.TryParse(IdInput, out var id)) StackResult = Bank.GetStack(id).ToString();
            });
            GetStackByNameCommand = new RelayCommand(_ => StackResult = Bank.GetStack(NameInput).ToString());
            DoActionByIdCommand = new RelayCommand(_ => {
                if (int.TryParse(IdInput, out var id)) ActionResult = Bank.DoActionById(id, ActionIndex, Offset) ? "✔ OK" : "✘ Failed";
            });
            DoActionByNameCommand = new RelayCommand(_ => ActionResult = Bank.DoActionByName(NameInput, ActionIndex, Offset) ? "✔ OK" : "✘ Failed");

            UpdateQuickFilter();
        }

        private static int[] ParseIds(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();
            var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new System.Collections.Generic.List<int>(parts.Length);
            foreach (var p in parts) if (int.TryParse(p.Trim(), out var id)) list.Add(id);
            return list.ToArray();
        }
        private static string[] ParseNames(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!Equals(field, value)) { field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); return true; }
            return false;
        }

        private void UpdateQuickFilter()
        {
            QuickStack = 0;
            var q = QuickFilterText;
            if (string.IsNullOrWhiteSpace(q)) { StackResult = string.Empty; return; }
            if (int.TryParse(q, out var id))
            {
                IdInput = q;
                var s = Bank.GetStack(id);
                QuickStack = (int)s;
                StackResult = s.ToString();
            }
            else
            {
                NameInput = q;
                var s = Bank.GetStack(q);
                QuickStack = (int)s;
                StackResult = s.ToString();
            }
        }

		private void OnPropertyChanged(string v)
		{
			if (string.IsNullOrEmpty(v)) return;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
		}
	}
}
