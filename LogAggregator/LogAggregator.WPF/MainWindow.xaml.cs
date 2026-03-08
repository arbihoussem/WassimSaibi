using System.Windows;
using LogAggregator.WPF.ViewModels;

namespace LogAggregator.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
