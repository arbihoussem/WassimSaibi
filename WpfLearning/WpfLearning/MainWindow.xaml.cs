using System.Windows;

namespace WpfLearning
{
    public partial class MainWindow : Window
    {
        private PersonViewModel _viewModel = new PersonViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }
    }
}