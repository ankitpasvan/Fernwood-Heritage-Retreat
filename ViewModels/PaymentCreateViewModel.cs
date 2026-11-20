using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels;

public class PaymentCreateViewModel
{
    [Required]
    public int BookingId { get; set; }

    public string BookingRef { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [Required]
    [StringLength(20)]
    public string Method { get; set; } = "Card";

    [StringLength(19)]
    public string? CardNumber { get; set; }

    [StringLength(100)]
    public string? CardName { get; set; }

    [StringLength(7)]
    public string? Expiry { get; set; }

    [StringLength(4)]
    public string? Cvv { get; set; }

    [StringLength(100)]
    public string? UpiId { get; set; }

    // Demo only: tick this to record a failed payment and test the retry flow.
    public bool SimulateFailure { get; set; }
}
