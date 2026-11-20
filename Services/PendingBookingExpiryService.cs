using HotelBookingSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotelBookingSystem.Services;


public class PendingBookingExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingBookingExpiryService> _logger;

    public PendingBookingExpiryService(IServiceScopeFactory scopeFactory, ILogger<PendingBookingExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay so the app finishes starting before the first sweep.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelExpiredPendingBookingsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to cancel expired pending bookings.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CancelExpiredPendingBookingsAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.Now;
        var expired = await context.Bookings
            .Where(b => b.Status == "Pending" && b.PendingExpiresAt != null && b.PendingExpiresAt <= now)
            .ToListAsync(stoppingToken);

        if (expired.Count == 0) return;

        foreach (var booking in expired)
        {
            booking.Status = "Cancelled";
        }

        await context.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Cancelled {Count} expired pending booking(s).", expired.Count);
    }
}
