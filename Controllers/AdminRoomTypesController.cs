using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminRoomTypesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminRoomTypesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var types = await _context.RoomTypes
            .Include(rt => rt.Rooms)
            .OrderBy(rt => rt.Name)
            .ToListAsync();
        return View(types);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var roomType = await _context.RoomTypes
            .Include(rt => rt.Rooms)
            .FirstOrDefaultAsync(rt => rt.RoomTypeId == id);
        if (roomType == null) return NotFound();
        ViewBag.AmenityNames = await _context.RoomTypeAmenities
            .Where(rta => rta.RoomTypeId == id)
            .Include(rta => rta.Amenity)
            .Select(rta => rta.Amenity!.Name)
            .ToListAsync();
        return View(roomType);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,PricePerNight,SizeSqFt,BedType,ViewType,MaxAdults,MaxChildren,MaxGuests,ImageUrl,IsActive")] RoomType roomType,
        int[] selectedAmenities)
    {
        if (await _context.RoomTypes.AnyAsync(rt => rt.Name == roomType.Name))
        {
            ModelState.AddModelError("Name", "A room type with this name already exists.");
        }

        var amenityIds = (selectedAmenities ?? Array.Empty<int>()).Distinct().ToList();
        if (amenityIds.Count > 0)
        {
            int validCount = await _context.Amenities.CountAsync(a => amenityIds.Contains(a.AmenityId));
            if (validCount != amenityIds.Count)
            {
                ModelState.AddModelError(string.Empty, "One or more selected amenities are invalid. Please choose amenities from the list.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            ViewBag.SelectedAmenities = amenityIds;
            return View(roomType);
        }

        _context.RoomTypes.Add(roomType);
        await _context.SaveChangesAsync();

        foreach (int amenityId in amenityIds)
        {
            _context.RoomTypeAmenities.Add(new RoomTypeAmenity
            {
                RoomTypeId = roomType.RoomTypeId,
                AmenityId = amenityId
            });
        }
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var roomType = await _context.RoomTypes.FindAsync(id);
        if (roomType == null) return NotFound();
        ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
        ViewBag.SelectedAmenities = await _context.RoomTypeAmenities
            .Where(rta => rta.RoomTypeId == id)
            .Select(rta => rta.AmenityId)
            .ToListAsync();
        return View(roomType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("RoomTypeId,Name,Description,PricePerNight,SizeSqFt,BedType,ViewType,MaxAdults,MaxChildren,MaxGuests,ImageUrl,IsActive")] RoomType roomType,
        int[] selectedAmenities)
    {
        if (id != roomType.RoomTypeId) return NotFound();

        if (await _context.RoomTypes.AnyAsync(rt => rt.Name == roomType.Name && rt.RoomTypeId != id))
        {
            ModelState.AddModelError("Name", "A room type with this name already exists.");
        }

        var amenityIds = (selectedAmenities ?? Array.Empty<int>()).Distinct().ToList();
        if (amenityIds.Count > 0)
        {
            int validCount = await _context.Amenities.CountAsync(a => amenityIds.Contains(a.AmenityId));
            if (validCount != amenityIds.Count)
            {
                ModelState.AddModelError(string.Empty, "One or more selected amenities are invalid. Please choose amenities from the list.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            ViewBag.SelectedAmenities = amenityIds;
            return View(roomType);
        }

        var existing = await _context.RoomTypes.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = roomType.Name;
        existing.Description = roomType.Description;
        existing.PricePerNight = roomType.PricePerNight;
        existing.SizeSqFt = roomType.SizeSqFt;
        existing.BedType = roomType.BedType;
        existing.ViewType = roomType.ViewType;
        existing.MaxAdults = roomType.MaxAdults;
        existing.MaxChildren = roomType.MaxChildren;
        existing.MaxGuests = roomType.MaxGuests;
        existing.ImageUrl = roomType.ImageUrl;
        existing.IsActive = roomType.IsActive;

        var oldMappings = _context.RoomTypeAmenities.Where(rta => rta.RoomTypeId == id);
        _context.RoomTypeAmenities.RemoveRange(oldMappings);
        foreach (int amenityId in amenityIds)
        {
            _context.RoomTypeAmenities.Add(new RoomTypeAmenity { RoomTypeId = id, AmenityId = amenityId });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var roomType = await _context.RoomTypes
            .Include(rt => rt.Rooms)
            .FirstOrDefaultAsync(rt => rt.RoomTypeId == id);
        if (roomType == null) return NotFound();
        ViewBag.HasBookings = await _context.Bookings
            .AnyAsync(b => b.Room != null && b.Room.RoomTypeId == id);
        return View(roomType);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var roomType = await _context.RoomTypes
            .Include(rt => rt.Rooms)
            .FirstOrDefaultAsync(rt => rt.RoomTypeId == id);
        if (roomType == null) return NotFound();

        if (roomType.Rooms.Any())
        {
            ModelState.AddModelError(string.Empty, "Cannot delete this room type because rooms of this type exist. Delete the rooms first.");
            ViewBag.HasBookings = await _context.Bookings.AnyAsync(b => b.Room != null && b.Room.RoomTypeId == id);
            return View(roomType);
        }

        _context.RoomTypes.Remove(roomType);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
