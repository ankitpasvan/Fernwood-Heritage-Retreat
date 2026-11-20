using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;


namespace HotelBookingSystem.Models;


public class Review

{

    [Key]

    public int ReviewId { get; set; }


    [ForeignKey("Booking")]

    public int BookingId { get; set; }


    [ForeignKey("RoomType")]

    public int RoomTypeId { get; set; }


    [Required]

    [StringLength(200)]

    public string Title { get; set; } = string.Empty;


    [Range(1, 5)]

    public int Rating { get; set; } = 5;


    [StringLength(1000)]

    public string? Comment { get; set; }


    public bool IsApproved { get; set; } = false;


    public DateTime CreatedAt { get; set; } = DateTime.Now;


    public Booking? Booking { get; set; }


    public RoomType? RoomType { get; set; }

}
