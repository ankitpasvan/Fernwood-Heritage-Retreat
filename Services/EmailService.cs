using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace HotelBookingSystem.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Email not sent: recipient address is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost)
            || string.IsNullOrWhiteSpace(_settings.SmtpUser)
            || string.IsNullOrWhiteSpace(_settings.SmtpPassword)
            || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            _logger.LogWarning("Email not sent to {ToEmail}: SMTP settings are incomplete. Configure EmailSettings (Gmail App Password) to enable emails.", toEmail);
            return false;
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            message.To.Add(new MailAddress(toEmail, string.IsNullOrWhiteSpace(toName) ? toEmail : toName));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
            client.EnableSsl = _settings.EnableSsl;
            client.Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {ToEmail}: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            // Never throw: email failure must not break booking/payment.
            _logger.LogError(ex, "Failed to send email to {ToEmail}: {Subject}", toEmail, subject);
            return false;
        }
    }
}
