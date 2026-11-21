using HotelBookingSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminEnquiriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminEnquiriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /AdminEnquiries
    public async Task<IActionResult> Index()
    {
        var enquiries = await _context.ContactEnquiries
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        ViewBag.UnreadCount = enquiries.Count(e => !e.IsRead);
        return View(enquiries);
    }

    // POST: /AdminEnquiries/MarkRead/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var enquiry = await _context.ContactEnquiries.FindAsync(id);
        if (enquiry == null)
        {
            return NotFound();
        }

        enquiry.IsRead = true;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Enquiry marked as read.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /AdminEnquiries/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var enquiry = await _context.ContactEnquiries.FindAsync(id);
        if (enquiry == null)
        {
            return NotFound();
        }

        _context.ContactEnquiries.Remove(enquiry);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Enquiry deleted.";
        return RedirectToAction(nameof(Index));
    }
}
