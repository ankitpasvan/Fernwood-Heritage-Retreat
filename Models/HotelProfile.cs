using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models;

public class HotelProfile
{
    [Key]
    public int HotelProfileId { get; set; }

    [Required]
    [StringLength(100)]
    public string HotelName { get; set; } = "Fernwood Heritage Retreat";

    [StringLength(200)]
    public string Tagline { get; set; } = "A quiet heritage stay above the valley";

    [StringLength(300)]
    public string Address { get; set; } = "Fernwood Estate, Jharipani Road, Mussoorie Hills";

    [StringLength(50)]
    public string City { get; set; } = "Mussoorie";

    [StringLength(50)]
    public string State { get; set; } = "Uttarakhand";

    [StringLength(20)]
    public string Phone { get; set; } = "+91 98765 43210";

    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = "stay@fernwoodheritage.in";

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    public string? HeroImageUrl { get; set; }

    public TimeOnly CheckInTime { get; set; } = new TimeOnly(14, 0);

    public TimeOnly CheckOutTime { get; set; } = new TimeOnly(11, 0);
}
