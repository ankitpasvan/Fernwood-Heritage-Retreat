using HotelBookingSystem.Models;

namespace HotelBookingSystem.ViewModels;

public class ReviewsIndexViewModel
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<Review> Reviews { get; set; } = new();
}
