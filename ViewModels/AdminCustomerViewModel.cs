namespace HotelBookingSystem.ViewModels;

public class AdminCustomerViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BookingCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastBookingDate { get; set; }
}
