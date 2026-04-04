using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace HotelReservationApp.Models
{
    public class ReservationBook : ObservableObject
    {
   public ObservableCollection<Reservation> Reservations { get; set; } = new();
    }
}
