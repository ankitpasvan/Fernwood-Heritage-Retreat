using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models;

public class Booking
{
    [Key]
    public int BookingId { get; set; }

    [Required]
    [StringLength(20)]
    public string BookingRef { get; set; } = string.Empty;

    [ForeignKey("User")]
    public int UserId { get; set; }

    [ForeignKey("Room")]
    public int RoomId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    public int Adults { get; set; } = 2;

    public int Children { get; set; } = 0;

    public int Guests { get; set; } = 2;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime? PendingExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public User? User { get; set; }

    public Room? Room { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public Review? Review { get; set; }
}
