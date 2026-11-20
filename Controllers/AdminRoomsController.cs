using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminRoomsController : Controller
{
    private readonly ApplicationDbContext _context;
    private static readonly string[] Statuses = { "Available", "Maintenance", "OutOfService" };

    public AdminRoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var rooms = await _context.Rooms
            .Include(r => r.RoomType)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();
        return View(rooms);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.RoomTypeId = new SelectList(await _context.RoomTypes.OrderBy(rt => rt.Name).ToListAsync(), "RoomTypeId", "Name");
        ViewBag.Statuses = new SelectList(Statuses);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RoomNumber,RoomTypeId,Status")] Room room)
    {
        if (await _context.Rooms.AnyAsync(r => r.RoomNumber == room.RoomNumber))
        {
            ModelState.AddModelError("RoomNumber", "A room with this number already exists.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.RoomTypeId = new SelectList(await _context.RoomTypes.OrderBy(rt => rt.Name).ToListAsync(), "RoomTypeId", "Name", room.RoomTypeId);
            ViewBag.Statuses = new SelectList(Statuses, room.Status);
            return View(room);
        }

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        ViewBag.RoomTypeId = new SelectList(await _context.RoomTypes.OrderBy(rt => rt.Name).ToListAsync(), "RoomTypeId", "Name", room.RoomTypeId);
        ViewBag.Statuses = new SelectList(Statuses, room.Status);
        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("RoomId,RoomNumber,RoomTypeId,Status")] Room room)
    {
        if (id != room.RoomId) return NotFound();

        if (await _context.Rooms.AnyAsync(r => r.RoomNumber == room.RoomNumber && r.RoomId != id))
        {
            ModelState.AddModelError("RoomNumber", "A room with this number already exists.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.RoomTypeId = new SelectList(await _context.RoomTypes.OrderBy(rt => rt.Name).ToListAsync(), "RoomTypeId", "Name", room.RoomTypeId);
            ViewBag.Statuses = new SelectList(Statuses, room.Status);
            return View(room);
        }

        var existing = await _context.Rooms.FindAsync(id);
        if (existing == null) return NotFound();

        existing.RoomNumber = room.RoomNumber;
        existing.RoomTypeId = room.RoomTypeId;
        existing.Status = room.Status;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var room = await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomId == id);
        if (room == null) return NotFound();
        ViewBag.BookingCount = await _context.Bookings.CountAsync(b => b.RoomId == id);
        return View(room);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var room = await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomId == id);
        if (room == null) return NotFound();

        if (await _context.Bookings.AnyAsync(b => b.RoomId == id))
        {
            ModelState.AddModelError(string.Empty, "Cannot delete this room because it has bookings. Set its status to OutOfService instead.");
            ViewBag.BookingCount = await _context.Bookings.CountAsync(b => b.RoomId == id);
            return View(room);
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
