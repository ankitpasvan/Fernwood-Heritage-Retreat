using System.Globalization;
using System.Text;
using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string? status)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        from ??= today.AddDays(-30);
        to ??= today;
        if (to < from) (from, to) = (to, from);

        var bookings = await GetFilteredBookingsAsync(from.Value, to.Value, status);

        ViewBag.From = from.Value.ToString("yyyy-MM-dd");
        ViewBag.To = to.Value.ToString("yyyy-MM-dd");
        ViewBag.Status = status ?? "";
        ViewBag.TotalBookings = bookings.Count;
        ViewBag.TotalRevenue = bookings.SelectMany(b => b.Payments).Where(p => p.Status == "Success").Sum(p => p.Amount);
        ViewBag.ConfirmedCount = bookings.Count(b => b.Status == "Confirmed");
        ViewBag.PendingCount = bookings.Count(b => b.Status == "Pending");
        ViewBag.CheckedInCount = bookings.Count(b => b.Status == "CheckedIn");
        ViewBag.CheckedOutCount = bookings.Count(b => b.Status == "CheckedOut");
        ViewBag.CancelledCount = bookings.Count(b => b.Status == "Cancelled");

        return View(bookings);
    }

    public async Task<IActionResult> ExportCsv(DateOnly? from, DateOnly? to, string? status)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        from ??= today.AddDays(-30);
        to ??= today;
        if (to < from) (from, to) = (to, from);

        var bookings = await GetFilteredBookingsAsync(from.Value, to.Value, status);

        var csv = new StringBuilder();
        csv.AppendLine("BookingRef,GuestName,GuestEmail,RoomType,RoomNumber,CheckInDate,CheckOutDate,Adults,Children,Guests,TotalAmount,Status,CreatedAt");
        foreach (var b in bookings)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv(b.BookingRef),
                Csv(b.User?.FullName),
                Csv(b.User?.Email),
                Csv(b.Room?.RoomType?.Name),
                Csv(b.Room?.RoomNumber),
                b.CheckInDate.ToString("yyyy-MM-dd"),
                b.CheckOutDate.ToString("yyyy-MM-dd"),
                b.Adults.ToString(CultureInfo.InvariantCulture),
                b.Children.ToString(CultureInfo.InvariantCulture),
                b.Guests.ToString(CultureInfo.InvariantCulture),
                b.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                Csv(b.Status),
                b.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }));
        }

        string fileName = $"Fernwood-Bookings-{from:yyyyMMdd}-{to:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private async Task<List<Booking>> GetFilteredBookingsAsync(DateOnly from, DateOnly to, string? status)
    {
        var fromDateTime = from.ToDateTime(TimeOnly.MinValue);
        var toDateTime = to.ToDateTime(TimeOnly.MaxValue);

        var query = _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.Payments)
            .Where(b => b.CreatedAt >= fromDateTime && b.CreatedAt <= toDateTime);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
