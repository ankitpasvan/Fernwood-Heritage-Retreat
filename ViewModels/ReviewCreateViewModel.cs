using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels;

public class ReviewCreateViewModel
{
    public int BookingId { get; set; }
    public int RoomTypeId { get; set; }
    public string BookingRef { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [StringLength(1000)]
    public string? Comment { get; set; }
}
