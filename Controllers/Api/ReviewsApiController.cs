using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers.Api;

[ApiController]
[Route("api/reviews")]
public class ReviewsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReviewsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/reviews (approved only, with average rating and count)
    [HttpGet]
    public async Task<IActionResult> GetApproved()
    {
        var reviews = await _context.Reviews
            .Include(r => r.Booking).ThenInclude(b => b!.User)
            .Include(r => r.RoomType)
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.ReviewId,
                r.Title,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                GuestName = r.Booking!.User!.FullName,
                RoomTypeName = r.RoomType!.Name
            })
            .ToListAsync();

        return Ok(new
        {
            totalReviews = reviews.Count,
            averageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0,
            reviews
        });
    }

    // POST: /api/reviews (login required; only your own checked-out booking, once)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.RoomType)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.BookingId == request.BookingId);

        if (booking == null) return NotFound(new { message = "Booking not found." });
        if (booking.UserId != userId) return Forbid();
        if (booking.Status != "CheckedOut") return BadRequest(new { message = "Only a checked-out booking can be reviewed." });
        if (booking.Review != null) return BadRequest(new { message = "This booking already has a review." });
        if (booking.Room?.RoomType == null) return BadRequest(new { message = "Booking details are incomplete." });

        var review = new Review
        {
            BookingId = booking.BookingId,
            RoomTypeId = booking.Room.RoomTypeId,
            Title = request.Title.Trim(),
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            IsApproved = false,
            CreatedAt = DateTime.Now
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            reviewId = review.ReviewId,
            status = "PendingApproval",
            message = "Review submitted. It will appear after admin approval."
        });
    }
}

public record CreateReviewRequest(
    [Required] int BookingId,
    [Required, StringLength(200)] string Title,
    [Range(1, 5)] int Rating,
    [StringLength(1000)] string? Comment);
