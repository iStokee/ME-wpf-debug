using MESharp.Commands;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string RequestedSection { get; set; }

        public ICommand ShowApiDocsCommand { get; }
        public ICommand ShowHowToUseCommand { get; }
        public ICommand ShowThemeSettingsCommand { get; }

        public HelpViewModel()
        {
            ShowApiDocsCommand = new RelayCommand(_ => ShowApiDocs());
            ShowHowToUseCommand = new RelayCommand(ShowHowToUse);
            ShowThemeSettingsCommand = new RelayCommand(_ => ShowThemeSettings());

            // Set the initial view
            ShowApiDocs();
        }

        private void ShowApiDocs()
        {
            CurrentViewModel = new ApiDocsViewModel();
        }

        private void ShowHowToUse(object section)
        {
            var sectionName = section as string;
            CurrentViewModel = new HowToUseViewModel(sectionName);
        }

        private void ShowThemeSettings()
        {
            CurrentViewModel = new ThemeSettingsViewModel();
        }
    }
}