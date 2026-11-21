using HotelBookingSystem.Models;

namespace HotelBookingSystem.Services;

public interface IBookingEmailService
{
    Task SendBookingConfirmationAsync(Booking booking);
    Task SendBookingCancellationAsync(Booking booking);
}
