using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Models;

[PrimaryKey(nameof(RoomTypeId), nameof(AmenityId))]
public class RoomTypeAmenity
{
    public int RoomTypeId { get; set; }

    public int AmenityId { get; set; }

    public RoomType? RoomType { get; set; }

    public Amenity? Amenity { get; set; }
}
