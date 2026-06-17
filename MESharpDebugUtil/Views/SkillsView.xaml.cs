using System.ComponentModel;
using System.Windows.Controls;
using MESharp.ViewModels;

namespace MESharp.Views
{
	/// <summary>
	/// Interaction logic for SkillsView.xaml
	/// </summary>
	public partial class SkillsView : UserControl
	{
		public SkillsView()
		{
			InitializeComponent();
		}

        private void SkillsTable_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (DataContext is not SkillsViewModel vm)
            {
                return;
            }

            if (!vm.TrySetSortFromMember(e.Column.SortMemberPath, e.Column.SortDirection))
            {
                return;
            }

            e.Handled = true;

            if (sender is not DataGrid grid)
            {
                return;
            }

            foreach (var column in grid.Columns)
            {
                column.SortDirection = null;
            }

            e.Column.SortDirection = vm.SortDescending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
    }
}
