using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers.Api;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingEmailService _bookingEmailService;

    public BookingsApiController(ApplicationDbContext context, IBookingEmailService bookingEmailService)
    {
        _context = context;
        _bookingEmailService = bookingEmailService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: /api/bookings/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Where(b => b.UserId == CurrentUserId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.BookingId,
                b.BookingRef,
                b.Status,
                b.CheckInDate,
                b.CheckOutDate,
                b.Adults,
                b.Children,
                b.Guests,
                b.TotalAmount,
                b.PendingExpiresAt,
                b.CreatedAt,
                RoomNumber = b.Room!.RoomNumber,
                RoomTypeName = b.Room!.RoomType!.Name
            })
            .ToListAsync();

        return Ok(bookings);
    }

    // GET: /api/bookings/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null) return NotFound(new { message = "Booking not found." });
        if (booking.UserId != CurrentUserId && !IsAdmin) return Forbid();

        await ExpireIfNeededAsync(booking);

        return Ok(new
        {
            booking.BookingId,
            booking.BookingRef,
            booking.Status,
            booking.CheckInDate,
            booking.CheckOutDate,
            booking.Adults,
            booking.Children,
            booking.Guests,
            booking.TotalAmount,
            booking.PendingExpiresAt,
            booking.CreatedAt,
            RoomNumber = booking.Room!.RoomNumber,
            RoomTypeName = booking.Room!.RoomType!.Name,
            GuestName = booking.User!.FullName,
            GuestEmail = booking.User!.Email
        });
    }

    // POST: /api/bookings
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        var type = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.RoomTypeId == request.RoomTypeId && rt.IsActive);
        if (type == null) return NotFound(new { message = "The selected room type is not available." });

        var today = DateOnly.FromDateTime(DateTime.Today);
        string? error = ValidateDatesAndGuests(request.CheckInDate, request.CheckOutDate, request.Adults, request.Children, type, today);
        if (error != null) return BadRequest(new { message = error });

        int nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        decimal total = type.PricePerNight * nights;

        // Serializable transaction: the availability re-check and the insert happen
        // atomically, so two customers cannot take the same room for overlapping dates.
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        Room? room = await FindFreeRoomAsync(type.RoomTypeId, request.CheckInDate, request.CheckOutDate);
        if (room == null)
        {
            await tx.RollbackAsync();
            return Conflict(new { message = "Sorry, this room type just got booked for the selected dates. Please try different dates." });
        }

        var booking = new Booking
        {
            BookingRef = "TEMP",
            UserId = CurrentUserId,
            RoomId = room.RoomId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Adults = request.Adults,
            Children = request.Children,
            Guests = request.Adults + request.Children,
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

        return StatusCode(StatusCodes.Status201Created, new
        {
            booking.BookingId,
            booking.BookingRef,
            RoomNumber = room.RoomNumber,
            RoomTypeName = type.Name,
            booking.CheckInDate,
            booking.CheckOutDate,
            Nights = nights,
            booking.TotalAmount,
            booking.Status,
            booking.PendingExpiresAt,
            message = "Booking created. Complete the demo payment within 15 minutes to confirm it."
        });
    }

    // POST: /api/bookings/1/cancel
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id);
        if (booking == null) return NotFound(new { message = "Booking not found." });
        if (booking.UserId != CurrentUserId && !IsAdmin) return Forbid();

        await ExpireIfNeededAsync(booking);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (booking.Status != "Pending" && booking.Status != "Confirmed")
        {
            return BadRequest(new { message = $"A booking with status '{booking.Status}' cannot be cancelled." });
        }
        if (booking.CheckInDate <= today)
        {
            return BadRequest(new { message = "This booking can no longer be cancelled because the check-in date has arrived." });
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

        return Ok(new
        {
            booking.BookingRef,
            status = "Cancelled",
            message = "Booking has been cancelled. The room is free for other guests now."
        });
    }

    private static string? ValidateDatesAndGuests(DateOnly checkIn, DateOnly checkOut, int adults, int children, RoomType type, DateOnly today)
    {
        if (checkIn < today) return "Check-in date cannot be in the past.";
        if (checkOut <= checkIn) return "Check-out date must be after check-in date.";
        if (adults < 1) return "At least 1 adult is required.";
        if (adults > 10) return "Adults cannot be more than 10.";
        if (children < 0) return "Children cannot be negative.";
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

public record CreateBookingRequest(
    [Required] int RoomTypeId,
    [Required] DateOnly CheckInDate,
    [Required] DateOnly CheckOutDate,
    [Range(1, 10)] int Adults,
    [Range(0, 10)] int Children);
