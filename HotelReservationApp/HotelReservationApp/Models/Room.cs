using CommunityToolkit.Mvvm.ComponentModel;

namespace HotelReservationApp.Models
{
    public class Room : ObservableObject
    {
        public int RoomID { get; set; }
        public string Type { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
