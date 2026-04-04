using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace HotelReservationApp.Models
{
    public class Hotel : ObservableObject
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public ObservableCollection<Room> Rooms { get; set; } = new();  
    }
}
