using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelBookingSystem.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingEmailService _bookingEmailService;

    public PaymentsController(ApplicationDbContext context, IBookingEmailService bookingEmailService)
    {
        _context = context;
        _bookingEmailService = bookingEmailService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: /Payments/Create?bookingId=1
    [HttpGet]
    public async Task<IActionResult> Create(int? bookingId)
    {
        if (bookingId == null) return NotFound();

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
        if (booking == null) return NotFound();
        if (booking.UserId != CurrentUserId && !IsAdmin) return Forbid();

        await ExpireIfNeededAsync(booking);

        if (booking.Status == "Confirmed")
        {
            TempData["SuccessMessage"] = $"Booking {booking.BookingRef} is already paid and confirmed.";
            return RedirectToAction("Details", "Bookings", new { id = booking.BookingId });
        }
        if (booking.Status != "Pending")
        {
            TempData["ErrorMessage"] = $"Payment is not possible for a booking with status '{booking.Status}'.";
            return RedirectToAction("Details", "Bookings", new { id = booking.BookingId });
        }

        var model = new PaymentCreateViewModel
        {
            BookingId = booking.BookingId,
            BookingRef = booking.BookingRef,
            Amount = booking.TotalAmount,
            Method = "Card"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == model.BookingId);
        if (booking == null) return NotFound();
        if (booking.UserId != CurrentUserId && !IsAdmin) return Forbid();

        await ExpireIfNeededAsync(booking);

        if (booking.Status == "Confirmed")
        {
            TempData["SuccessMessage"] = $"Booking {booking.BookingRef} is already paid and confirmed.";
            return RedirectToAction("Details", "Bookings", new { id = booking.BookingId });
        }
        if (booking.Status != "Pending")
        {
            TempData["ErrorMessage"] = $"Payment is not possible for a booking with status '{booking.Status}'.";
            return RedirectToAction("Details", "Bookings", new { id = booking.BookingId });
        }

        // Never trust the amount from the form: always use the booking total.
        model.BookingRef = booking.BookingRef;
        model.Amount = booking.TotalAmount;

        if (model.Method != "Card" && model.Method != "UPI")
        {
            ModelState.AddModelError("Method", "Please choose Card or UPI.");
        }
        if (model.Method == "Card")
        {
            if (string.IsNullOrWhiteSpace(model.CardNumber)) ModelState.AddModelError("CardNumber", "Card number is required for card payment.");
            if (string.IsNullOrWhiteSpace(model.CardName)) ModelState.AddModelError("CardName", "Name on card is required.");
            if (string.IsNullOrWhiteSpace(model.Expiry)) ModelState.AddModelError("Expiry", "Expiry (MM/YY) is required.");
            if (string.IsNullOrWhiteSpace(model.Cvv)) ModelState.AddModelError("Cvv", "CVV is required.");
        }
        if (model.Method == "UPI" && string.IsNullOrWhiteSpace(model.UpiId))
        {
            ModelState.AddModelError("UpiId", "UPI ID is required for UPI payment.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        int attemptNo = await _context.Payments.CountAsync(p => p.BookingId == booking.BookingId) + 1;

        // Demo failure path: the booking stays Pending (Option A) so the customer can retry.
        if (model.SimulateFailure)
        {
            var failed = new Payment
            {
                BookingId = booking.BookingId,
                AttemptNo = attemptNo,
                Amount = booking.TotalAmount,
                Method = model.Method,
                Status = "Failed",
                TransactionRef = "MOCK-FAIL-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                CreatedAt = DateTime.Now
            };
            _context.Payments.Add(failed);
            await _context.SaveChangesAsync();

            TempData["ErrorMessage"] = "Demo payment failed (simulated). Your booking is still Pending - you can try the payment again within the 15-minute hold.";
            return RedirectToAction("Details", "Bookings", new { id = booking.BookingId });
        }

        var payment = new Payment
        {
            BookingId = booking.BookingId,
            AttemptNo = attemptNo,
            Amount = booking.TotalAmount,
            Method = model.Method,
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
            TempData["ErrorMessage"] = "This booking already has a successful payment recorded. Please check the booking details.";
            return RedirectToAction("Details", "Bookings", new { id = booking.BookingId });
        }


        var confirmedBooking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingId == booking.BookingId);
        if (confirmedBooking != null)
        {
            await _bookingEmailService.SendBookingConfirmationAsync(confirmedBooking);
        }

        TempData["SuccessMessage"] = $"Demo payment successful. Booking {booking.BookingRef} is confirmed. (No real money was charged - this is a mock payment for the college demo.)";
        return RedirectToAction("Details", "Bookings", new { id = booking.BookingId });
    }

    // GET: /Payments/History
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var payments = await _context.Payments
            .Include(p => p.Booking)
            .Where(p => p.Booking!.UserId == CurrentUserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(payments);
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
