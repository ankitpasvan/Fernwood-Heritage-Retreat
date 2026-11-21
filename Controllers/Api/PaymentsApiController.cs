using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers.Api;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingEmailService _bookingEmailService;

    public PaymentsApiController(ApplicationDbContext context, IBookingEmailService bookingEmailService)
    {
        _context = context;
        _bookingEmailService = bookingEmailService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: /api/payments/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var payments = await _context.Payments
            .Include(p => p.Booking)
            .Where(p => p.Booking!.UserId == CurrentUserId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.PaymentId,
                p.BookingId,
                BookingRef = p.Booking!.BookingRef,
                p.AttemptNo,
                p.Amount,
                p.Method,
                p.Status,
                p.TransactionRef,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(payments);
    }

    // POST: /api/payments (mock payment — no real money, no card details collected)
    [HttpPost]
    public async Task<IActionResult> Pay([FromBody] PayRequest request)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == request.BookingId);
        if (booking == null) return NotFound(new { message = "Booking not found." });
        if (booking.UserId != CurrentUserId && !IsAdmin) return Forbid();

        await ExpireIfNeededAsync(booking);

        if (booking.Status == "Confirmed")
        {
            return Conflict(new { message = $"Booking {booking.BookingRef} is already paid and confirmed." });
        }
        if (booking.Status != "Pending")
        {
            return BadRequest(new { message = $"Payment is not possible for a booking with status '{booking.Status}'." });
        }

        if (request.Method != "Card" && request.Method != "UPI")
        {
            return BadRequest(new { message = "Please choose Card or UPI." });
        }

        int attemptNo = await _context.Payments.CountAsync(p => p.BookingId == booking.BookingId) + 1;

        // Demo failure path: the booking stays Pending (Option A) so the customer can retry.
        if (request.SimulateFailure)
        {
            var failed = new Payment
            {
                BookingId = booking.BookingId,
                AttemptNo = attemptNo,
                Amount = booking.TotalAmount,
                Method = request.Method,
                Status = "Failed",
                TransactionRef = "MOCK-FAIL-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                CreatedAt = DateTime.Now
            };
            _context.Payments.Add(failed);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                booking.BookingRef,
                status = "Failed",
                bookingStatus = "Pending",
                message = "Demo payment failed (simulated). Your booking is still Pending - you can try the payment again within the 15-minute hold."
            });
        }

        var payment = new Payment
        {
            BookingId = booking.BookingId,
            AttemptNo = attemptNo,
            Amount = booking.TotalAmount,
            Method = request.Method,
            Status = "Success",
            TransactionRef = "MOCK-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            CreatedAt = DateTime.Now
        };

        booking.Status = "Confirmed";
        booking.PendingExpiresAt = null;

        _context.Payments.Add(payment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "This booking already has a successful payment recorded. Please check the booking details." });
        }

        // Booking is now Confirmed and saved. Only now send the confirmation email.
        var confirmedBooking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingId == booking.BookingId);
        if (confirmedBooking != null)
        {
            await _bookingEmailService.SendBookingConfirmationAsync(confirmedBooking);
        }

        return Ok(new
        {
            booking.BookingRef,
            status = "Success",
            bookingStatus = "Confirmed",
            payment.TransactionRef,
            message = "Demo payment successful. Booking is confirmed. (No real money was charged - this is a mock payment for the college demo.)"
        });
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

public record PayRequest(
    [Required] int BookingId,
    [Required] string Method,
    bool SimulateFailure = false);
