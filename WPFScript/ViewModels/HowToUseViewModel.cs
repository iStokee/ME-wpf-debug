namespace MESharp.ViewModels
{
    public class HowToUseViewModel : BaseViewModel
    {
        public string Section { get; }

        public HowToUseViewModel(string section)
        {
            Section = section;
        }
    }
}