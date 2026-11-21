using HotelBookingSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers.Api;

[ApiController]
[Route("api/rooms")]
public class RoomsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RoomsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/rooms
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _context.RoomTypes
            .Where(rt => rt.IsActive)
            .OrderBy(rt => rt.PricePerNight)
            .ToListAsync();

        var typeIds = types.Select(t => t.RoomTypeId).ToList();

        var mappings = await _context.RoomTypeAmenities
            .Include(m => m.Amenity)
            .Where(m => typeIds.Contains(m.RoomTypeId))
            .ToListAsync();

        var result = types.Select(rt => new
        {
            rt.RoomTypeId,
            rt.Name,
            rt.Description,
            rt.PricePerNight,
            rt.SizeSqFt,
            rt.BedType,
            rt.ViewType,
            rt.MaxAdults,
            rt.MaxChildren,
            rt.MaxGuests,
            rt.ImageUrl,
            Amenities = mappings.Where(m => m.RoomTypeId == rt.RoomTypeId)
                .Select(m => m.Amenity!.Name).ToList()
        }).ToList();

        return Ok(result);
    }

    // GET: /api/rooms/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var type = await _context.RoomTypes
            .FirstOrDefaultAsync(rt => rt.RoomTypeId == id && rt.IsActive);

        if (type == null) return NotFound(new { message = "Room type not found." });

        var amenityNames = await _context.RoomTypeAmenities
            .Where(m => m.RoomTypeId == type.RoomTypeId)
            .Select(m => m.Amenity!.Name)
            .ToListAsync();

        return Ok(new
        {
            type.RoomTypeId,
            type.Name,
            type.Description,
            type.PricePerNight,
            type.SizeSqFt,
            type.BedType,
            type.ViewType,
            type.MaxAdults,
            type.MaxChildren,
            type.MaxGuests,
            type.ImageUrl,
            Amenities = amenityNames
        });
    }

    // GET: /api/rooms/search?checkIn=2026-11-25&checkOut=2026-11-27&adults=2&children=0
    [HttpGet("search")]
    public async Task<IActionResult> Search(DateOnly? checkIn, DateOnly? checkOut, int adults = 2, int children = 0)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        checkIn ??= today;
        checkOut ??= today.AddDays(1);

        if (checkIn < today) return BadRequest(new { message = "Check-in date cannot be in the past." });
        if (checkOut <= checkIn) return BadRequest(new { message = "Check-out date must be after check-in date." });
        if (adults < 1 || adults > 10) return BadRequest(new { message = "Adults must be between 1 and 10." });
        if (children < 0 || children > 10) return BadRequest(new { message = "Children must be between 0 and 10." });

        int nights = checkOut.Value.DayNumber - checkIn.Value.DayNumber;
        var now = DateTime.Now;

        var types = await _context.RoomTypes
            .Where(rt => rt.IsActive
                && adults <= rt.MaxAdults
                && children <= rt.MaxChildren
                && adults + children <= rt.MaxGuests)
            .OrderBy(rt => rt.PricePerNight)
            .ToListAsync();

        var typeIds = types.Select(t => t.RoomTypeId).ToList();
        var rooms = await _context.Rooms
            .Where(r => typeIds.Contains(r.RoomTypeId))
            .ToListAsync();

        var blockedRoomIds = (await _context.Bookings
            .Where(b => (b.Status == "Confirmed" || b.Status == "CheckedIn"
                || (b.Status == "Pending" && (b.PendingExpiresAt == null || b.PendingExpiresAt > now)))
                && b.CheckInDate < checkOut.Value && checkIn.Value < b.CheckOutDate)
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync()).ToHashSet();

        var results = types
            .Select(t => new
            {
                t.RoomTypeId,
                t.Name,
                t.Description,
                t.PricePerNight,
                TotalPrice = t.PricePerNight * nights,
                Nights = nights,
                t.SizeSqFt,
                t.BedType,
                t.ViewType,
                t.MaxGuests,
                t.ImageUrl,
                AvailableRooms = rooms.Count(r => r.RoomTypeId == t.RoomTypeId
                    && r.Status == "Available"
                    && !blockedRoomIds.Contains(r.RoomId))
            })
            .Where(r => r.AvailableRooms > 0)
            .ToList();

        return Ok(results);
    }
}
