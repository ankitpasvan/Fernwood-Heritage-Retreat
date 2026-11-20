using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

public class RoomsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var types = await _context.RoomTypes
            .Where(rt => rt.IsActive)
            .OrderBy(rt => rt.PricePerNight)
            .ToListAsync();
        var cards = await BuildCardsAsync(types, null, null);
        return View(cards);
    }

    public async Task<IActionResult> Details(int? id, DateOnly? checkIn, DateOnly? checkOut, int adults = 2, int children = 0)
    {
        if (id == null) return NotFound();
        var type = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.RoomTypeId == id && rt.IsActive);
        if (type == null) return NotFound();
        var cards = await BuildCardsAsync(new List<RoomType> { type }, checkIn, checkOut);
        ViewBag.CheckIn = checkIn?.ToString("yyyy-MM-dd");
        ViewBag.CheckOut = checkOut?.ToString("yyyy-MM-dd");
        ViewBag.Adults = adults;
        ViewBag.Children = children;
        return View(cards[0]);
    }

    public async Task<IActionResult> Search(DateOnly? checkIn, DateOnly? checkOut, int adults = 2, int children = 0, int guests = 0)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var model = new RoomSearchViewModel
        {
            CheckInDate = checkIn ?? today,
            CheckOutDate = checkOut ?? today.AddDays(1),
            Adults = adults,
            Children = children
        };

        if (guests > 0 && children == 0 && guests > adults)
        {
            model.Children = guests - adults;
        }

        if (checkIn == null || checkOut == null)
        {
            return View(model);
        }

        if (model.CheckInDate < today)
        {
            ModelState.AddModelError("CheckInDate", "Check-in date cannot be in the past.");
        }
        if (model.CheckOutDate <= model.CheckInDate)
        {
            ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date.");
        }
        if (model.Adults < 1)
        {
            ModelState.AddModelError("Adults", "At least 1 adult is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Searched = true;

        var types = await _context.RoomTypes
            .Where(rt => rt.IsActive
                && model.Adults <= rt.MaxAdults
                && model.Children <= rt.MaxChildren
                && model.Guests <= rt.MaxGuests)
            .OrderBy(rt => rt.PricePerNight)
            .ToListAsync();

        var cards = await BuildCardsAsync(types, model.CheckInDate, model.CheckOutDate);
        model.Results = cards.Where(c => c.AvailableRooms > 0).ToList();
        return View(model);
    }

    private async Task<List<RoomTypeCardViewModel>> BuildCardsAsync(
        List<RoomType> types, DateOnly? checkIn, DateOnly? checkOut)
    {
        var now = DateTime.Now;
        var typeIds = types.Select(t => t.RoomTypeId).ToList();

        var rooms = await _context.Rooms
            .Where(r => typeIds.Contains(r.RoomTypeId))
            .ToListAsync();

        var mappings = await _context.RoomTypeAmenities
            .Include(rta => rta.Amenity)
            .Where(rta => typeIds.Contains(rta.RoomTypeId))
            .ToListAsync();

        HashSet<int> blockedRoomIds = new();
        if (checkIn != null && checkOut != null)
        {
            blockedRoomIds = (await _context.Bookings
                .Where(b => (b.Status == "Confirmed" || b.Status == "CheckedIn"
                    || (b.Status == "Pending" && (b.PendingExpiresAt == null || b.PendingExpiresAt > now)))
                    && b.CheckInDate < checkOut.Value && checkIn.Value < b.CheckOutDate)
                .Select(b => b.RoomId)
                .Distinct()
                .ToListAsync()).ToHashSet();
        }

        var cards = new List<RoomTypeCardViewModel>();
        foreach (var type in types)
        {
            int nights = (checkIn != null && checkOut != null && checkOut > checkIn)
                ? checkOut.Value.DayNumber - checkIn.Value.DayNumber : 0;

            int available = rooms.Count(r => r.RoomTypeId == type.RoomTypeId
                && r.Status == "Available"
                && !blockedRoomIds.Contains(r.RoomId));

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
                AvailableRooms = available,
                TotalPrice = type.PricePerNight * nights
            });
        }

        return cards;
    }
}
