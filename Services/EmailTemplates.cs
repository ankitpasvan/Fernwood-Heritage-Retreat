using HotelBookingSystem.Models;
using System.Net;

namespace HotelBookingSystem.Services;

public static class EmailTemplates
{
    public static string BookingConfirmationHtml(Booking booking)
    {
        var name = WebUtility.HtmlEncode(booking.User?.FullName ?? "Guest");
        var roomTypeName = WebUtility.HtmlEncode(booking.Room?.RoomType?.Name ?? "Room");
        var roomNumber = WebUtility.HtmlEncode(booking.Room?.RoomNumber ?? "—");
        var nights = booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber;
        var successPayment = booking.Payments?
            .Where(p => p.Status == "Success")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();
        var paymentText = successPayment == null
            ? "Pending"
            : $"Paid — Demo {successPayment.Method} (Ref: {successPayment.TransactionRef})";

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
<title>Booking Confirmed</title>
</head>
<body style=""margin:0;padding:0;background-color:#FAF6EF;font-family:Arial,Helvetica,sans-serif;color:#2B2B2B;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FAF6EF;padding:24px 12px;"">
<tr><td align=""center"">
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;width:100%;background-color:#ffffff;border:1px solid #EDE6D8;"">
<tr><td style=""background-color:#7A1F1F;padding:28px 24px;text-align:center;"">
<p style=""margin:0;color:#C9A86A;font-size:12px;letter-spacing:3px;"">MUSSOORIE HILLS &bull; UTTARAKHAND</p>
<p style=""margin:8px 0 0;color:#ffffff;font-family:Georgia,serif;font-size:28px;"">Fernwood Heritage Retreat</p>
<p style=""margin:6px 0 0;color:#EDE6D8;font-size:13px;font-style:italic;"">A quiet heritage stay above the valley</p>
</td></tr>
<tr><td style=""padding:28px 28px 8px;"">
<p style=""margin:0 0 8px;font-size:16px;"">Dear {name},</p>
<p style=""margin:0 0 16px;font-size:15px;line-height:1.6;"">Your booking is <strong>confirmed</strong>. We look forward to welcoming you to Fernwood.</p>
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-top:1px solid #EDE6D8;"">
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Booking Reference</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-weight:bold;color:#7A1F1F;font-size:14px;"">{booking.BookingRef}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Room Type</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{roomTypeName}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Room Number</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{roomNumber}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Check-in</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{booking.CheckInDate:dd MMM yyyy} &bull; 2:00 PM</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Check-out</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{booking.CheckOutDate:dd MMM yyyy} &bull; 11:00 AM</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Nights</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{nights}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Guests</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{booking.Adults} Adults &bull; {booking.Children} Children (Total {booking.Guests})</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Total Amount</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-weight:bold;font-size:15px;"">₹{booking.TotalAmount:N0}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Payment Status</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{paymentText}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Booking Status</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-weight:bold;font-size:14px;"">Confirmed</td></tr>
</table>
</td></tr>
<tr><td style=""padding:20px 28px;"">
<p style=""margin:0 0 12px;padding:12px;background-color:#FAF6EF;border-left:3px solid #C9A86A;font-size:13px;line-height:1.6;color:#6B6B6B;"">Demo booking: this project uses a mock payment system. No real money was charged.</p>
<p style=""margin:0;font-size:13px;line-height:1.7;color:#6B6B6B;"">Fernwood Estate, Jharipani Road, Mussoorie Hills, Mussoorie, Uttarakhand<br />Phone: +91 98765 43210 &nbsp;|&nbsp; Email: stay@fernwoodheritage.in<br />Check-in 2:00 PM &nbsp;|&nbsp; Check-out 11:00 AM</p>
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>";
    }

    public static string BookingCancellationHtml(Booking booking)
    {
        var name = WebUtility.HtmlEncode(booking.User?.FullName ?? "Guest");
        var roomTypeName = WebUtility.HtmlEncode(booking.Room?.RoomType?.Name ?? "Room");
        var roomNumber = WebUtility.HtmlEncode(booking.Room?.RoomNumber ?? "—");
        var nights = booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber;
        var hadSuccessPayment = booking.Payments?.Any(p => p.Status == "Success") == true;
        var refundText = hadSuccessPayment
            ? "Your payment was a demo (mock) payment — no real money was charged, so no refund applies."
            : "No payment was made for this booking, so no refund applies.";

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
<title>Booking Cancelled</title>
</head>
<body style=""margin:0;padding:0;background-color:#FAF6EF;font-family:Arial,Helvetica,sans-serif;color:#2B2B2B;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FAF6EF;padding:24px 12px;"">
<tr><td align=""center"">
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;width:100%;background-color:#ffffff;border:1px solid #EDE6D8;"">
<tr><td style=""background-color:#7A1F1F;padding:28px 24px;text-align:center;"">
<p style=""margin:0;color:#C9A86A;font-size:12px;letter-spacing:3px;"">MUSSOORIE HILLS &bull; UTTARAKHAND</p>
<p style=""margin:8px 0 0;color:#ffffff;font-family:Georgia,serif;font-size:28px;"">Fernwood Heritage Retreat</p>
<p style=""margin:6px 0 0;color:#EDE6D8;font-size:13px;font-style:italic;"">A quiet heritage stay above the valley</p>
</td></tr>
<tr><td style=""padding:28px 28px 8px;"">
<p style=""margin:0 0 8px;font-size:16px;"">Dear {name},</p>
<p style=""margin:0 0 16px;font-size:15px;line-height:1.6;"">Your booking has been <strong>cancelled</strong>. The room is now free for other guests.</p>
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-top:1px solid #EDE6D8;"">
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Booking Reference</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-weight:bold;color:#7A1F1F;font-size:14px;"">{booking.BookingRef}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Room Type</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{roomTypeName}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Room Number</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{roomNumber}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Check-in</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{booking.CheckInDate:dd MMM yyyy} &bull; 2:00 PM</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Check-out</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{booking.CheckOutDate:dd MMM yyyy} &bull; 11:00 AM</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Nights</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{nights}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Guests</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-size:14px;"">{booking.Adults} Adults &bull; {booking.Children} Children (Total {booking.Guests})</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Total Amount</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-weight:bold;font-size:15px;"">₹{booking.TotalAmount:N0}</td></tr>
<tr><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;color:#6B6B6B;font-size:14px;"">Booking Status</td><td style=""padding:10px 0;border-bottom:1px solid #EDE6D8;text-align:right;font-weight:bold;font-size:14px;"">Cancelled</td></tr>
</table>
</td></tr>
<tr><td style=""padding:20px 28px;"">
<p style=""margin:0 0 12px;padding:12px;background-color:#FAF6EF;border-left:3px solid #C9A86A;font-size:13px;line-height:1.6;color:#6B6B6B;"">{refundText}</p>
<p style=""margin:0 0 12px;font-size:14px;line-height:1.6;"">If you cancelled by mistake, please book again or contact us — we will be happy to help.</p>
<p style=""margin:0;font-size:13px;line-height:1.7;color:#6B6B6B;"">Fernwood Estate, Jharipani Road, Mussoorie Hills, Mussoorie, Uttarakhand<br />Phone: +91 98765 43210 &nbsp;|&nbsp; Email: stay@fernwoodheritage.in<br />Check-in 2:00 PM &nbsp;|&nbsp; Check-out 11:00 AM</p>
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>";
    }
}
