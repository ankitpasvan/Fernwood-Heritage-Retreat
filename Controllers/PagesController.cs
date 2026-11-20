using Microsoft.AspNetCore.Mvc;

namespace HotelBookingSystem.Controllers;

public class PagesController : Controller
{
    [Route("Dining")]
    public IActionResult Dining()
    {
        return View();
    }

    [Route("Experiences")]
    public IActionResult Experiences()
    {
        return View();
    }

    [Route("Gallery")]
    public IActionResult Gallery()
    {
        return View();
    }

    [Route("About")]
    public IActionResult About()
    {
        return View();
    }

    [Route("Contact")]
    public IActionResult Contact()
    {
        return View();
    }
}
