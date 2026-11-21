using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminAmenitiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminAmenitiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var amenities = await _context.Amenities
            .Include(a => a.RoomTypeAmenities)
            .OrderBy(a => a.Name)
            .ToListAsync();
        return View(amenities);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Icon,Description")] Amenity amenity)
    {
        if (await _context.Amenities.AnyAsync(a => a.Name == amenity.Name))
        {
            ModelState.AddModelError("Name", "An amenity with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(amenity);
        }

        _context.Amenities.Add(amenity);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Amenity added successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var amenity = await _context.Amenities.FindAsync(id);
        if (amenity == null) return NotFound();
        return View(amenity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("AmenityId,Name,Icon,Description")] Amenity amenity)
    {
        if (id != amenity.AmenityId) return NotFound();

        if (await _context.Amenities.AnyAsync(a => a.Name == amenity.Name && a.AmenityId != id))
        {
            ModelState.AddModelError("Name", "An amenity with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(amenity);
        }

        var existing = await _context.Amenities.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = amenity.Name;
        existing.Icon = amenity.Icon;
        existing.Description = amenity.Description;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Amenity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var amenity = await _context.Amenities
            .Include(a => a.RoomTypeAmenities)
            .FirstOrDefaultAsync(a => a.AmenityId == id);
        if (amenity == null) return NotFound();
        return View(amenity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var amenity = await _context.Amenities
            .Include(a => a.RoomTypeAmenities)
            .FirstOrDefaultAsync(a => a.AmenityId == id);
        if (amenity == null) return NotFound();

        if (amenity.RoomTypeAmenities.Any())
        {
            ModelState.AddModelError(string.Empty, "Cannot delete this amenity because room types use it. Remove it from the room types first.");
            return View(amenity);
        }

        _context.Amenities.Remove(amenity);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Amenity deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
