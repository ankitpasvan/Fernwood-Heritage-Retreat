namespace HotelBookingSystem.ViewModels;

public class RoomTypeCardViewModel
{
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerNight { get; set; }
    public int SizeSqFt { get; set; }
    public string BedType { get; set; } = string.Empty;
    public string ViewType { get; set; } = string.Empty;
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public int MaxGuests { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> AmenityNames { get; set; } = new();
    public int AvailableRooms { get; set; }
    public decimal TotalPrice { get; set; }
}
