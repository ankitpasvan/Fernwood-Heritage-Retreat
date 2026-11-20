using HotelBookingSystem.Models;

namespace HotelBookingSystem.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CheckedInBookings { get; set; }
    public int CheckedOutBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRoomsToday { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingReviews { get; set; }
    public List<Booking> RecentBookings { get; set; } = new();
    public List<Booking> TodayCheckIns { get; set; } = new();
    public List<Booking> TodayCheckOuts { get; set; } = new();
}
