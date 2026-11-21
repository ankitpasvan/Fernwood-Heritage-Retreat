using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models;

public class RoomType
{
    [Key]
    public int RoomTypeId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    [Range(1, 1000000, ErrorMessage = "Price per night must be between 1 and 10,00,000.")]
    public decimal PricePerNight { get; set; }

    [Range(1, 100000, ErrorMessage = "Size must be between 1 and 1,00,000 sq.ft.")]
    public int SizeSqFt { get; set; }

    [StringLength(50)]
    public string BedType { get; set; } = "Double Bed";

    [StringLength(50)]
    public string ViewType { get; set; } = "Valley View";

    [Range(1, 10, ErrorMessage = "Max adults must be between 1 and 10.")]
    public int MaxAdults { get; set; } = 2;

    [Range(0, 10, ErrorMessage = "Max children must be between 0 and 10.")]
    public int MaxChildren { get; set; } = 1;

    [Range(1, 10, ErrorMessage = "Max guests must be between 1 and 10.")]
    public int MaxGuests { get; set; } = 3;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
