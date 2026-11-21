using System.ComponentModel.DataAnnotations;
using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [Route("Faq")]
    public IActionResult Faq()
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

    [HttpPost]
    [Route("Subscribe")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
        {
            TempData["SubscribeMessage"] = "Please enter a valid email address.";
            return RedirectToLocalReferer();
        }

        var alreadySubscribed = await _context.ContactEnquiries
            .AnyAsync(e => e.Email == email && e.Name == "Newsletter Subscriber");
        if (alreadySubscribed)
        {
            TempData["SubscribeMessage"] = "This email is already subscribed to Private Offers.";
            return RedirectToLocalReferer();
        }

        _context.ContactEnquiries.Add(new ContactEnquiry
        {
            Name = "Newsletter Subscriber",
            Email = email,
            Message = "Subscribed to Private Offers from the footer.",
            CreatedAt = DateTime.Now,
            IsRead = false
        });
        await _context.SaveChangesAsync();

        TempData["SubscribeMessage"] = "Thank you! You are subscribed to Private Offers.";
        return RedirectToLocalReferer();
    }

    private IActionResult RedirectToLocalReferer()
    {
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer)
            && Uri.TryCreate(referer, UriKind.Absolute, out var uri)
            && uri.Host == Request.Host.Host)
        {
            return Redirect(uri.PathAndQuery);
        }
        return RedirectToAction("Index", "Home");
    }
}
