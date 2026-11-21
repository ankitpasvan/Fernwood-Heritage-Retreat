using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingSystem.Controllers;

public class PagesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Route("Dining")]
    public IActionResult Dining()
    {
        return View();
    }

    [Route("Experiences")]
    public IActionResult Experiences()
    {
        return View();
    }

    [Route("Gallery")]
    public IActionResult Gallery()
    {
        return View();
    }

    [Route("About")]
    public IActionResult About()
    {
        return View();
    }

    [Route("Contact")]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [Route("Contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactEnquiry enquiry)
    {
        if (!ModelState.IsValid)
        {
            return View(enquiry);
        }

        enquiry.CreatedAt = DateTime.Now;
        enquiry.IsRead = false;
        _context.ContactEnquiries.Add(enquiry);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you! Your message has been received. We will get back to you soon.";
        return RedirectToAction("Contact");
    }
}
