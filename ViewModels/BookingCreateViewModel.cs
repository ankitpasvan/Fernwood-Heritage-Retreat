using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels;

public class BookingCreateViewModel
{
    [Required]
    public int RoomTypeId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Range(1, 10)]
    public int Adults { get; set; } = 2;

    [Range(0, 10)]
    public int Children { get; set; } = 0;

    public int Guests => Adults + Children;

    // Assigned room (filled on the confirm page; re-checked on submit).
    public int RoomId { get; set; }

    // Display only (recalculated on the server on submit, never trusted from the form).
    public string RoomTypeName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int SizeSqFt { get; set; }

    public string BedType { get; set; } = string.Empty;

    public string ViewType { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public int Nights { get; set; }

    public decimal TotalAmount { get; set; }

    public string RoomNumber { get; set; } = string.Empty;
}
