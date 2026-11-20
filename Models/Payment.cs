using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [ForeignKey("Booking")]
    public int BookingId { get; set; }

    public int AttemptNo { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(20)]
    public string Method { get; set; } = "Card";

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [StringLength(100)]
    public string? TransactionRef { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Booking? Booking { get; set; }
}
