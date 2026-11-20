using System.Diagnostics;
using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var types = await _context.RoomTypes
            .Where(rt => rt.IsActive)
            .OrderBy(rt => rt.PricePerNight)
            .ToListAsync();

        var typeIds = types.Select(t => t.RoomTypeId).ToList();

        var rooms = await _context.Rooms
            .Where(r => typeIds.Contains(r.RoomTypeId))
            .ToListAsync();

        var mappings = await _context.RoomTypeAmenities
            .Include(rta => rta.Amenity)
            .Where(rta => typeIds.Contains(rta.RoomTypeId))
            .ToListAsync();

        var cards = new List<RoomTypeCardViewModel>();

        foreach (var type in types)
        {
            cards.Add(new RoomTypeCardViewModel
            {
                RoomTypeId = type.RoomTypeId,
                Name = type.Name,
                Description = type.Description,
                PricePerNight = type.PricePerNight,
                SizeSqFt = type.SizeSqFt,
                BedType = type.BedType,
                ViewType = type.ViewType,
                MaxAdults = type.MaxAdults,
                MaxChildren = type.MaxChildren,
                MaxGuests = type.MaxGuests,
                ImageUrl = type.ImageUrl,
                AmenityNames = mappings.Where(m => m.RoomTypeId == type.RoomTypeId)
                    .Select(m => m.Amenity!.Name).ToList(),
                AvailableRooms = rooms.Count(r => r.RoomTypeId == type.RoomTypeId && r.Status == "Available"),
                TotalPrice = 0
            });
        }

        ViewBag.ReviewCount = await _context.Reviews.CountAsync(r => r.IsApproved);

        return View(cards);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
