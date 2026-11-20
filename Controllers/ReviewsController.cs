using System.Security.Claims;
using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Reviews — public page with approved reviews only
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var reviews = await _context.Reviews
            .Include(r => r.Booking).ThenInclude(b => b.User)
            .Include(r => r.RoomType)
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var model = new ReviewsIndexViewModel
        {
            Reviews = reviews,
            TotalReviews = reviews.Count,
            AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0
        };

        return View(model);
    }

    // GET: /Reviews/Create?bookingId=5
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Create(int bookingId)
    {
        var booking = await GetEligibleBookingAsync(bookingId);
        if (booking == null)
        {
            TempData["ErrorMessage"] = "This booking is not eligible for a review. Only your own checked-out bookings can be reviewed, once.";
            return RedirectToAction("MyReviews");
        }

        var model = new ReviewCreateViewModel
        {
            BookingId = booking.BookingId,
            RoomTypeId = booking.Room!.RoomTypeId,
            BookingRef = booking.BookingRef,
            RoomTypeName = booking.Room.RoomType!.Name,
            GuestName = booking.User!.FullName,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            Rating = 5
        };

        return View(model);
    }

    // POST: /Reviews/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model)
    {
        var booking = await GetEligibleBookingAsync(model.BookingId);
        if (booking == null)
        {
            TempData["ErrorMessage"] = "This booking is not eligible for a review. Only your own checked-out bookings can be reviewed, once.";
            return RedirectToAction("MyReviews");
        }

        if (!ModelState.IsValid)
        {
            model.BookingRef = booking.BookingRef;
            model.RoomTypeName = booking.Room!.RoomType!.Name;
            model.GuestName = booking.User!.FullName;
            model.CheckInDate = booking.CheckInDate;
            model.CheckOutDate = booking.CheckOutDate;
            return View(model);
        }

        var review = new Review
        {
            BookingId = booking.BookingId,
            RoomTypeId = booking.Room!.RoomTypeId,
            Title = model.Title.Trim(),
            Rating = model.Rating,
            Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
            IsApproved = false,
            CreatedAt = DateTime.Now
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you! Your review has been submitted and will appear after admin approval.";
        return RedirectToAction("MyReviews");
    }

    // GET: /Reviews/MyReviews
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyReviews()
    {
        int userId = GetCurrentUserId();

        var myReviews = await _context.Reviews
            .Include(r => r.Booking).ThenInclude(b => b.Room).ThenInclude(r => r.RoomType)
            .Include(r => r.RoomType)
            .Where(r => r.Booking!.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var reviewedBookingIds = myReviews.Select(r => r.BookingId).ToList();

        var eligibleBookings = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .Where(b => b.UserId == userId && b.Status == "CheckedOut" && !reviewedBookingIds.Contains(b.BookingId))
            .OrderByDescending(b => b.CheckOutDate)
            .ToListAsync();

        ViewBag.EligibleBookings = eligibleBookings;
        return View(myReviews);
    }

    // GET: /Reviews/Edit/3
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var review = await GetOwnReviewAsync(id.Value);
        if (review == null) return NotFound();

        var model = new ReviewCreateViewModel
        {
            BookingId = review.BookingId,
            RoomTypeId = review.RoomTypeId,
            BookingRef = review.Booking!.BookingRef,
            RoomTypeName = review.RoomType!.Name,
            GuestName = review.Booking.User!.FullName,
            CheckInDate = review.Booking.CheckInDate,
            CheckOutDate = review.Booking.CheckOutDate,
            Title = review.Title,
            Rating = review.Rating,
            Comment = review.Comment
        };

        ViewBag.ReviewId = review.ReviewId;
        return View(model);
    }

    // POST: /Reviews/Edit/3
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReviewCreateViewModel model)
    {
        var review = await GetOwnReviewAsync(id);
        if (review == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.BookingRef = review.Booking!.BookingRef;
            model.RoomTypeName = review.RoomType!.Name;
            model.GuestName = review.Booking.User!.FullName;
            model.CheckInDate = review.Booking.CheckInDate;
            model.CheckOutDate = review.Booking.CheckOutDate;
            ViewBag.ReviewId = review.ReviewId;
            return View(model);
        }

        review.Title = model.Title.Trim();
        review.Rating = model.Rating;
        review.Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();
        review.IsApproved = false;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your review was updated and sent for approval again.";
        return RedirectToAction("MyReviews");
    }

    // POST: /Reviews/Delete/3
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await GetOwnReviewAsync(id);
        if (review == null) return NotFound();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your review has been deleted.";
        return RedirectToAction("MyReviews");
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // A booking is reviewable only when it is the current user's, CheckedOut, and has no review yet.
    private async Task<Booking?> GetEligibleBookingAsync(int bookingId)
    {
        int userId = GetCurrentUserId();

        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .Include(b => b.User)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null) return null;
        if (booking.UserId != userId) return null;
        if (booking.Status != "CheckedOut") return null;
        if (booking.Review != null) return null;

        return booking;
    }

    private async Task<Review?> GetOwnReviewAsync(int reviewId)
    {
        int userId = GetCurrentUserId();

        return await _context.Reviews
            .Include(r => r.Booking).ThenInclude(b => b.User)
            .Include(r => r.Booking).ThenInclude(b => b.Room).ThenInclude(r => r.RoomType)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.Booking!.UserId == userId);
    }
}
