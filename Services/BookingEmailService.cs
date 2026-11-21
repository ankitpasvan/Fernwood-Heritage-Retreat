using HotelBookingSystem.Models;
using Microsoft.Extensions.Logging;

namespace HotelBookingSystem.Services;

public class BookingEmailService : IBookingEmailService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingEmailService> _logger;

    public BookingEmailService(IEmailService emailService, ILogger<BookingEmailService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendBookingConfirmationAsync(Booking booking)
    {
        try
        {
            // Safety: send only for a booking that is really Confirmed with a Success payment.
            // This is what stops duplicate confirmation emails on retries.
            if (booking.Status != "Confirmed") return;
            if (booking.User == null || string.IsNullOrWhiteSpace(booking.User.Email)) return;
            if (booking.Payments == null || !booking.Payments.Any(p => p.Status == "Success")) return;

            var subject = $"Booking Confirmed — {booking.BookingRef} | Fernwood Heritage Retreat";
            var html = EmailTemplates.BookingConfirmationHtml(booking);
            var sent = await _emailService.SendEmailAsync(booking.User.Email, booking.User.FullName, subject, html);
            if (!sent)
            {
                _logger.LogWarning("Booking confirmation email could not be sent for {BookingRef}.", booking.BookingRef);
            }
        }
        catch (Exception ex)
        {
            // Email failure must never break the booking flow.
            _logger.LogError(ex, "Error sending booking confirmation email for {BookingRef}.", booking.BookingRef);
        }
    }

    public async Task SendBookingCancellationAsync(Booking booking)
    {
        try
        {
            if (booking.Status != "Cancelled") return;
            if (booking.User == null || string.IsNullOrWhiteSpace(booking.User.Email)) return;

            var subject = $"Booking Cancelled — {booking.BookingRef} | Fernwood Heritage Retreat";
            var html = EmailTemplates.BookingCancellationHtml(booking);
            var sent = await _emailService.SendEmailAsync(booking.User.Email, booking.User.FullName, subject, html);
            if (!sent)
            {
                _logger.LogWarning("Booking cancellation email could not be sent for {BookingRef}.", booking.BookingRef);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking cancellation email for {BookingRef}.", booking.BookingRef);
        }
    }
}
