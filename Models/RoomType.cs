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
    public decimal PricePerNight { get; set; }

    public int SizeSqFt { get; set; }

    [StringLength(50)]
    public string BedType { get; set; } = "Double Bed";

    [StringLength(50)]
    public string ViewType { get; set; } = "Valley View";

    public int MaxAdults { get; set; } = 2;

    public int MaxChildren { get; set; } = 1;

    public int MaxGuests { get; set; } = 3;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
