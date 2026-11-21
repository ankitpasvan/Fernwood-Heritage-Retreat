using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models;

public class ContactEnquiry
{
    [Key]
    public int ContactEnquiryId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(15)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
