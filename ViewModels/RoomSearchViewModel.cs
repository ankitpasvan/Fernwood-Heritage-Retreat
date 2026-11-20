using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels;

public class RoomSearchViewModel
{
    [Required(ErrorMessage = "Check-in date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Check-in")]
    public DateOnly CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Check-out")]
    public DateOnly CheckOutDate { get; set; }

    [Range(1, 10, ErrorMessage = "Adults must be between 1 and 10.")]
    public int Adults { get; set; } = 2;

    [Range(0, 10, ErrorMessage = "Children must be between 0 and 10.")]
    public int Children { get; set; } = 0;

    public int Guests => Adults + Children;

    public int Nights => CheckOutDate > CheckInDate ? CheckOutDate.DayNumber - CheckInDate.DayNumber : 0;

    public bool Searched { get; set; }

    public List<RoomTypeCardViewModel> Results { get; set; } = new();
}
