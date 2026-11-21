using HotelBookingSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminUsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Phone,Role")] HotelBookingSystem.Models.User user)
    {
        if (id != user.UserId) return NotFound();

        if (user.Role != "Customer" && user.Role != "Admin")
        {
            ModelState.AddModelError("Role", "Role must be Customer or Admin.");
        }

        if (!ModelState.IsValid)
        {
            return View(user);
        }

        var existing = await _context.Users.FindAsync(id);
        if (existing == null) return NotFound();

        // Never leave the system without an admin.
        if (existing.Role == "Admin" && user.Role != "Admin"
            && !await _context.Users.AnyAsync(u => u.Role == "Admin" && u.UserId != id))
        {
            ModelState.AddModelError("Role", "Cannot remove the last admin.");
            return View(user);
        }

        existing.FullName = user.FullName;
        existing.Phone = user.Phone;
        existing.Role = user.Role;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "User updated successfully.";
        return RedirectToAction("Customers", "Admin");
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        ViewBag.BookingCount = await _context.Bookings.CountAsync(b => b.UserId == user.UserId);
        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.Role == "Admin")
        {
            ModelState.AddModelError(string.Empty, "Admin users cannot be deleted from here. Demote the user to Customer first, or keep the admin account.");
            ViewBag.BookingCount = await _context.Bookings.CountAsync(b => b.UserId == user.UserId);
            return View(user);
        }

        int bookingCount = await _context.Bookings.CountAsync(b => b.UserId == user.UserId);
        if (bookingCount > 0)
        {
            ModelState.AddModelError(string.Empty, "Cannot delete a customer who has bookings.");
            ViewBag.BookingCount = bookingCount;
            return View(user);
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "User deleted successfully.";
        return RedirectToAction("Customers", "Admin");
    }
}
