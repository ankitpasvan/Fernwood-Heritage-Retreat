namespace HotelBookingSystem.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
}
