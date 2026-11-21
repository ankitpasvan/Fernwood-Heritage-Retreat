using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace HotelBookingSystem.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingEmailService _bookingEmailService;

    public BookingsController(ApplicationDbContext context, IBookingEmailService bookingEmailService)
    {
        _context = context;
        _bookingEmailService = bookingEmailService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: /Bookings/Create?roomTypeId=1&checkIn=2026-11-25&checkOut=2026-11-27&adults=2&children=0
    [HttpGet]
    public async Task<IActionResult> Create(int? roomTypeId, DateOnly? checkIn, DateOnly? checkOut, int adults = 2, int children = 0)
    {
        if (roomTypeId == null)
        {
            TempData["ErrorMessage"] = "Please choose a room type first.";
            return RedirectToAction("Search", "Rooms");
        }

        var type = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.RoomTypeId == roomTypeId && rt.IsActive);
        if (type == null)
        {
            TempData["ErrorMessage"] = "The selected room type is not available.";
            return RedirectToAction("Search", "Rooms");
        }

        if (checkIn == null || checkOut == null)
        {
            TempData["ErrorMessage"] = "Please select your check-in and check-out dates first.";
            return RedirectToAction("Details", "Rooms", new { id = roomTypeId });
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        string? error = ValidateDatesAndGuests(checkIn.Value, checkOut.Value, adults, children, type, today);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction("Search", "Rooms");
        }

        var room = await FindFreeRoomAsync(type.RoomTypeId, checkIn.Value, checkOut.Value);
        if (room == null)
        {
            TempData["ErrorMessage"] = "No rooms of this type are available for the selected dates. Please try different dates.";
            return RedirectToAction("Search", "Rooms");
        }

        int nights = checkOut.Value.DayNumber - checkIn.Value.DayNumber;

        var model = new BookingCreateViewModel
        {
            RoomTypeId = type.RoomTypeId,
            RoomId = room.RoomId,
            CheckInDate = checkIn.Value,
            CheckOutDate = checkOut.Value,
            Adults = adults,
            Children = children,
            RoomTypeName = type.Name,
            ImageUrl = type.ImageUrl,
            SizeSqFt = type.SizeSqFt,
            BedType = type.BedType,
            ViewType = type.ViewType,
            PricePerNight = type.PricePerNight,
            Nights = nights,
            TotalAmount = type.PricePerNight * nights,
            RoomNumber = room.RoomNumber
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        var type = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.RoomTypeId == model.RoomTypeId && rt.IsActive);
        if (type == null)
        {
            TempData["ErrorMessage"] = "The selected room type is not available.";
            return RedirectToAction("Search", "Rooms");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        string? error = ValidateDatesAndGuests(model.CheckInDate, model.CheckOutDate, model.Adults, model.Children, type, today);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction("Search", "Rooms");
        }

        int nights = model.CheckOutDate.DayNumber - model.CheckInDate.DayNumber;
        decimal total = type.PricePerNight * nights;

        // Serializable transaction: the availability re-check and the insert happen
        // atomically, so two customers cannot take the same room for overlapping dates.
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        Room? room = null;
        if (model.RoomId > 0)
        {
            room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == model.RoomId
                && r.RoomTypeId == type.RoomTypeId && r.Status == "Available");
            if (room != null && await IsRoomBlockedAsync(room.RoomId, model.CheckInDate, model.CheckOutDate))
            {
                room = null;
            }
        }

        room ??= await FindFreeRoomAsync(type.RoomTypeId, model.CheckInDate, model.CheckOutDate);
        if (room == null)
        {
            await tx.RollbackAsync();
            TempData["ErrorMessage"] = "Sorry, this room type just got booked for the selected dates. Please try different dates.";
            return RedirectToAction("Search", "Rooms");
        }

        var booking = new Booking
        {
            BookingRef = "TEMP",
            UserId = CurrentUserId,
            RoomId = room.RoomId,
            CheckInDate = model.CheckInDate,
            CheckOutDate = model.CheckOutDate,
            Adults = model.Adults,
            Children = model.Children,
            Guests = model.Adults + model.Children,
            TotalAmount = total,
            Status = "Pending",
            PendingExpiresAt = DateTime.Now.AddMinutes(15),
            CreatedAt = DateTime.Now
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        booking.BookingRef = $"FH-{booking.BookingId:D6}";
        await _context.SaveChangesAsync();

        await tx.CommitAsync();

        TempData["SuccessMessage"] = $"Booking {booking.BookingRef} created. Complete the demo payment within 15 minutes to confirm it.";
        return RedirectToAction("Details", new { id = booking.BookingId });
    }

    // GET: /Bookings/Details/1
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.User)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null) return NotFound();
        if (booking.UserId != CurrentUserId && !IsAdmin) return Forbid();

        await ExpireIfNeededAsync(booking);

        return View(booking);
    }

    // GET: /Bookings/MyBookings
    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Where(b => b.UserId == CurrentUserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        bool changed = false;
        foreach (var b in bookings)
        {
            if (b.Status == "Pending" && b.PendingExpiresAt != null && b.PendingExpiresAt <= DateTime.Now)
            {
                b.Status = "Cancelled";
                changed = true;
            }
        }
        if (changed) await _context.SaveChangesAsync();

        return View(bookings);
    }

    // POST: /Bookings/Cancel/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id);
        if (booking == null) return NotFound();
        if (booking.UserId != CurrentUserId && !IsAdmin) return Forbid();

        await ExpireIfNeededAsync(booking);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (booking.Status != "Pending" && booking.Status != "Confirmed")
        {
            TempData["ErrorMessage"] = $"A booking with status '{booking.Status}' cannot be cancelled.";
            return RedirectToAction("Details", new { id });
        }
        if (booking.CheckInDate <= today)
        {
            TempData["ErrorMessage"] = "This booking can no longer be cancelled because the check-in date has arrived.";
            return RedirectToAction("Details", new { id });
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

        TempData["SuccessMessage"] = $"Booking {booking.BookingRef} has been cancelled. The room is free for other guests now.";
        return RedirectToAction("Details", new { id });
    }

    private static string? ValidateDatesAndGuests(DateOnly checkIn, DateOnly checkOut, int adults, int children, RoomType type, DateOnly today)
    {
        if (checkIn < today) return "Check-in date cannot be in the past.";
        if (checkOut <= checkIn) return "Check-out date must be after check-in date.";
        if (adults < 1) return "At least 1 adult is required.";
        if (adults > 10) return "Adults cannot be more than 10.";
        if (children < 0) return "Children count cannot be negative.";
        if (children > 10) return "Children cannot be more than 10.";
        if (adults > type.MaxAdults) return $"This room type allows at most {type.MaxAdults} adults.";
        if (children > type.MaxChildren) return $"This room type allows at most {type.MaxChildren} children.";
        if (adults + children > type.MaxGuests) return $"This room type allows at most {type.MaxGuests} guests in total.";
        return null;
    }

    private async Task<Room?> FindFreeRoomAsync(int roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        var rooms = await _context.Rooms
            .Where(r => r.RoomTypeId == roomTypeId && r.Status == "Available")
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        foreach (var room in rooms)
        {
            if (!await IsRoomBlockedAsync(room.RoomId, checkIn, checkOut))
            {
                return room;
            }
        }

        return null;
    }

    private async Task<bool> IsRoomBlockedAsync(int roomId, DateOnly checkIn, DateOnly checkOut)
    {
        var now = DateTime.Now;
        return await _context.Bookings.AnyAsync(b => b.RoomId == roomId
            && (b.Status == "Confirmed" || b.Status == "CheckedIn"
                || (b.Status == "Pending" && (b.PendingExpiresAt == null || b.PendingExpiresAt > now)))
            && b.CheckInDate < checkOut && checkIn < b.CheckOutDate);
    }

    private async Task ExpireIfNeededAsync(Booking booking)
    {
        if (booking.Status == "Pending" && booking.PendingExpiresAt != null && booking.PendingExpiresAt <= DateTime.Now)
        {
            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();
        }
    }
}
