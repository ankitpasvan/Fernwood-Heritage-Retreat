using HotelBookingSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminReviewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /AdminReviews?filter=pending
    [HttpGet]
    public async Task<IActionResult> Index(string? filter)
    {
        var query = _context.Reviews
            .Include(r => r.Booking).ThenInclude(b => b!.User)
            .Include(r => r.RoomType)
            .AsQueryable();

        if (filter == "pending")
        {
            query = query.Where(r => !r.IsApproved);
        }
        else if (filter == "approved")
        {
            query = query.Where(r => r.IsApproved);
        }

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        ViewBag.Filter = filter;
        ViewBag.PendingCount = await _context.Reviews.CountAsync(r => !r.IsApproved);
        return View(reviews);
    }

    // POST: /AdminReviews/Approve/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? filter)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null) return NotFound();

        review.IsApproved = true;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Review approved. It is now visible on the Reviews page.";
        return RedirectToAction("Index", new { filter });
    }

    // POST: /AdminReviews/Reject/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? filter)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null) return NotFound();

        review.IsApproved = false;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Review hidden. It will not appear on the Reviews page.";
        return RedirectToAction("Index", new { filter });
    }

    // POST: /AdminReviews/Delete/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? filter)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null) return NotFound();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Review deleted.";
        return RedirectToAction("Index", new { filter });
    }
}
