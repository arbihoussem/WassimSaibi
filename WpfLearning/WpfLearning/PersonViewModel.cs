using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfLearning
{
    public partial class PersonViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Introduction))]
        private string _name = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Introduction))]
        private string _city = "";

        public string Introduction => $"Hi! I'm {_name} from {_city}.";

        [RelayCommand]
        private void Clear()
        {
            Name = "";
            City = "";
        }
    }
}