using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HotelReservationApp.Models
{
    public class Reservation : ObservableObject 
        {
        public string GuestName { get; set; }
        public Room ReservedRoom { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        
    }
}
