using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models;

public class Amenity
{
    [Key]
    public int AmenityId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Icon { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
}
