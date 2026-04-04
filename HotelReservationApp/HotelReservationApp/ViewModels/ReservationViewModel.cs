using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelReservationApp.Models;
using System.Collections.ObjectModel;

namespace HotelReservationApp.ViewModels
{
    public partial class ReservationViewModel : ObservableObject
    {
        //list of reservations
        [ObservableProperty]
        private ObservableCollection<Reservation> reservations = new();

        //list of reservation (for delete/edit)
        [ObservableProperty]
        private Reservation selectedReservation;

        [ObservableProperty]
        private string guestName;

        [ObservableProperty]
        private int roomID;

        [ObservableProperty]
        private string roomType;

        [ObservableProperty]
        private DateTime checkIn = DateTime.Today;

        [ObservableProperty]
        private DateTime checkOut = DateTime.Today.AddDays(1);

        [RelayCommand]
        private void AddReservation()
        {
            var room = new Room
            {
                RoomID = roomID,
                Type = roomType,
                IsAvailable = true
            };

            var reservation = new Reservation
            {
                GuestName = guestName,
                ReservedRoom = room,
                CheckInDate = checkIn,
                CheckOutDate = checkOut
            };

            Reservations.Add(reservation);
        }
        // Delete selected reservation
        [RelayCommand]
            private void DeleteReservation()
            {
                if (selectedReservation != null)
                    Reservations.Remove(selectedReservation);
            }
        
    }
}
