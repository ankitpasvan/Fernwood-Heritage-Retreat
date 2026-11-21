using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingEmailService _bookingEmailService;

    public AdminController(ApplicationDbContext context, IBookingEmailService bookingEmailService)
    {
        _context = context;
        _bookingEmailService = bookingEmailService;
    }

    // GET: /Admin/Dashboard
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        await ExpirePendingBookingsAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var model = new AdminDashboardViewModel
        {
            TotalBookings = await _context.Bookings.CountAsync(),
            PendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending"),
            ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == "Confirmed"),
            CheckedInBookings = await _context.Bookings.CountAsync(b => b.Status == "CheckedIn"),
            CheckedOutBookings = await _context.Bookings.CountAsync(b => b.Status == "CheckedOut"),
            CancelledBookings = await _context.Bookings.CountAsync(b => b.Status == "Cancelled"),
            TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer"),
            TotalRooms = await _context.Rooms.CountAsync(),
            AvailableRooms = await _context.Rooms.CountAsync(r => r.Status == "Available"),
            OccupiedRoomsToday = await _context.Bookings
                .Where(b => (b.Status == "Confirmed" || b.Status == "CheckedIn")
                    && b.CheckInDate <= today && today < b.CheckOutDate)
                .Select(b => b.RoomId)
                .Distinct()
                .CountAsync(),
            TotalRevenue = await _context.Payments
                .Where(p => p.Status == "Success")
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved),
            RecentBookings = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(8)
                .ToListAsync(),
            TodayCheckIns = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Where(b => b.Status == "Confirmed" && b.CheckInDate == today)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync(),
            TodayCheckOuts = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Where(b => b.Status == "CheckedIn" && b.CheckOutDate == today)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync()
        };

        return View(model);
    }

    // GET: /Admin/Bookings?search=FH-000001&status=Confirmed
    [HttpGet]
    public async Task<IActionResult> Bookings(string? search, string? status)
    {
        await ExpirePendingBookingsAsync();

        var query = _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.User)
            .Include(b => b.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(b => b.BookingRef.Contains(s)
                || b.User!.FullName.Contains(s)
                || b.User!.Email.Contains(s));
        }

        var list = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(list);
    }

    // GET: /Admin/BookingDetails/1
    [HttpGet]
    public async Task<IActionResult> BookingDetails(int? id)
    {
        if (id == null) return NotFound();

        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.User)
            .Include(b => b.Payments)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null) return NotFound();

        await ExpirePendingBookingAsync(booking);

        return View(booking);
    }

    // POST: /Admin/CheckIn/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id);
        if (booking == null) return NotFound();

        if (booking.Status != "Confirmed")
        {
            TempData["ErrorMessage"] = $"Only a Confirmed booking can be checked in (current status: {booking.Status}).";
            return RedirectToAction("BookingDetails", new { id });
        }

        if (DateOnly.FromDateTime(DateTime.Today) < booking.CheckInDate)
        {
            TempData["ErrorMessage"] = "Check-in is not due yet. The check-in date is " + booking.CheckInDate.ToString("dd MMM yyyy") + ".";
            return RedirectToAction("BookingDetails", new { id });
        }

        booking.Status = "CheckedIn";
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Booking {booking.BookingRef} checked in.";
        return RedirectToAction("BookingDetails", new { id });
    }

    // POST: /Admin/CheckOut/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(int id)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id);
        if (booking == null) return NotFound();

        if (booking.Status != "CheckedIn")
        {
            TempData["ErrorMessage"] = $"Only a CheckedIn booking can be checked out (current status: {booking.Status}).";
            return RedirectToAction("BookingDetails", new { id });
        }

        booking.Status = "CheckedOut";
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Booking {booking.BookingRef} checked out. The guest can now submit a review.";
        return RedirectToAction("BookingDetails", new { id });
    }

    // POST: /Admin/Cancel/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id);
        if (booking == null) return NotFound();

        await ExpirePendingBookingAsync(booking);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (booking.Status != "Pending" && booking.Status != "Confirmed")
        {
            TempData["ErrorMessage"] = $"A booking with status '{booking.Status}' cannot be cancelled.";
            return RedirectToAction("BookingDetails", new { id });
        }
        if (booking.CheckInDate <= today)
        {
            TempData["ErrorMessage"] = "This booking can no longer be cancelled because the check-in date has arrived.";
            return RedirectToAction("BookingDetails", new { id });
        }

        booking.Status = "Cancelled";
        await _context.SaveChangesAsync();

        // Booking is now Cancelled and saved. Only now send the cancellation email.
        var cancelledBooking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingId == booking.BookingId);
        if (cancelledBooking != null)
        {
            await _bookingEmailService.SendBookingCancellationAsync(cancelledBooking);
        }

        TempData["SuccessMessage"] = $"Booking {booking.BookingRef} has been cancelled.";
        return RedirectToAction("BookingDetails", new { id });
    }

    // GET: /Admin/Customers?search=ankit
    [HttpGet]
    public async Task<IActionResult> Customers(string? search)
    {
        var query = _context.Users.Where(u => u.Role == "Customer").AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u => u.FullName.Contains(s) || u.Email.Contains(s));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userIds = users.Select(u => u.UserId).ToList();

        var bookings = await _context.Bookings
            .Where(b => userIds.Contains(b.UserId))
            .ToListAsync();

        var bookingIds = bookings.Select(b => b.BookingId).ToList();
        var successfulPayments = await _context.Payments
            .Where(p => p.Status == "Success" && bookingIds.Contains(p.BookingId))
            .ToListAsync();

        var bookingUser = bookings.ToDictionary(b => b.BookingId, b => b.UserId);

        var model = users.Select(u => new AdminCustomerViewModel
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            CreatedAt = u.CreatedAt,
            BookingCount = bookings.Count(b => b.UserId == u.UserId),
            TotalSpent = successfulPayments
                .Where(p => bookingUser.TryGetValue(p.BookingId, out int uid) && uid == u.UserId)
                .Sum(p => p.Amount),
            LastBookingDate = bookings
                .Where(b => b.UserId == u.UserId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => (DateTime?)b.CreatedAt)
                .FirstOrDefault()
        }).ToList();

        ViewBag.Search = search;
        return View(model);
    }

    // GET: /Admin/Payments?status=Success
    [HttpGet]
    public async Task<IActionResult> Payments(string? status)
    {
        var query = _context.Payments
            .Include(p => p.Booking).ThenInclude(b => b!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        ViewBag.Status = status;
        ViewBag.TotalRevenue = await _context.Payments
            .Where(p => p.Status == "Success")
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        return View(list);
    }

    // Expired Pending holds become Cancelled so stats and availability stay truthful.
    private async Task ExpirePendingBookingsAsync()
    {
        var now = DateTime.Now;
        var expired = await _context.Bookings
            .Where(b => b.Status == "Pending" && b.PendingExpiresAt != null && b.PendingExpiresAt <= now)
            .ToListAsync();

        if (expired.Count == 0) return;

        foreach (var b in expired)
        {
            b.Status = "Cancelled";
        }
        await _context.SaveChangesAsync();
    }

    private async Task ExpirePendingBookingAsync(Booking booking)
    {
        if (booking.Status == "Pending" && booking.PendingExpiresAt != null && booking.PendingExpiresAt <= DateTime.Now)
        {
            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();
        }
    }
}
