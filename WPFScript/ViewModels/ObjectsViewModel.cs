using MESharp.API;
using MESharp.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public class ObjectsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Objects.GameObject> AllObjects { get; }
            = new ObservableCollection<Objects.GameObject>();

        public ICollectionView ObjectsView { get; }

        private string _filterText = "";
        public string FilterText
        {
            get => _filterText;
            set { if (Set(ref _filterText, value)) ObjectsView.Refresh(); }
        }

        private int _actionIndex;
        public int ActionIndex { get => _actionIndex; set => Set(ref _actionIndex, value); }

        public ICommand RefreshCommand { get; }
        public ICommand DoActionCommand { get; }

        public ObjectsViewModel()
        {
            ObjectsView = CollectionViewSource.GetDefaultView(AllObjects);
            ObjectsView.Filter = FilterPredicate;

            RefreshCommand = new RelayCommand(_ => Refresh());
            DoActionCommand = new RelayCommand(_ => DoAction(), _ => !string.IsNullOrWhiteSpace(FilterText));

            Refresh();
        }

        private void Refresh()
        {
            var list = Objects.GetAll();
            AllObjects.Clear();
            foreach (var o in list) AllObjects.Add(o);
            ObjectsView.Refresh();
        }

        private bool FilterPredicate(object o)
        {
            var go = (Objects.GameObject)o;
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            return int.TryParse(FilterText, out var id)
                ? go.Id == id
                : go.Name?.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DoAction()
        {
            if (int.TryParse(FilterText, out var id))
                Objects.DoActionByIds(new[] { id }, ActionIndex);
            else
                Objects.DoActionByNames(new[] { FilterText }, ActionIndex);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!Equals(field, value)) { field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); return true; }
            return false;
        }
    }
}

