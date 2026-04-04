using HotelReservationApp.ViewModels;
using System.Windows;

namespace HotelReservationApp
{
    
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ReservationViewModel();
        }
    }
}