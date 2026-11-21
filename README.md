# Fernwood Heritage Retreat — Hotel Reservation and Booking Management System

Single-hotel reservation and booking system built with ASP.NET Core MVC, Entity Framework Core and SQL Server.

## Tech stack

- ASP.NET Core MVC (.NET 8), C#
- Entity Framework Core 8 (SQL Server)
- SQL Server Express (`.\SQLEXPRESS`), database `HotelBookingDB`
- Bootstrap 5.3.3, Playfair Display + Inter fonts
- Cookie authentication with Admin / Customer roles

## Features

### Guest

- Browse rooms with live availability search (check-in / check-out dates + guests)
- Room details with total price (PricePerNight x nights)
- Book a room (15-minute Pending hold), mock Card / UPI payment
- My Bookings: view bookings, pay a Pending booking, cancel a booking
- My Reviews: write a review for a checked-out stay, edit or delete own reviews
- Public Reviews page: approved reviews only, with real average rating and count
- Contact form: messages are saved to the database

### Admin

- Dashboard: bookings, revenue, occupancy, today's check-ins / check-outs, pending reviews
- Room types and rooms CRUD; room status Available / Maintenance / OutOfService
- Bookings management: check-in, check-out, cancel
- Customers and payments lists
- Review approval: approve, hide, delete
- Enquiries: view contact messages, mark as read, delete

## Business rules

- Single hotel only: one `HotelProfile` row (Id = 1). Admin cannot add a second hotel.
- Availability = Room.Status is Available AND no overlapping Confirmed / CheckedIn / Pending-unexpired booking.
- Pending bookings expire after 15 minutes (background service + lazy expiry on page open).
- A failed payment keeps the booking Pending so the customer can retry inside the 15-minute hold; it is cancelled only on expiry.
- Double-booking is prevented with a Serializable transaction + availability re-check.
- Room status is operational only. "Occupied" is derived from bookings and is never stored.
- Reviews are allowed only for the customer's own CheckedOut bookings, one review per booking, and appear publicly only after admin approval.
- TotalAmount = PricePerNight x nights. No taxes.

## Database (10 tables)

`Users`, `HotelProfiles`, `RoomTypes`, `Rooms`, `Amenities`, `RoomTypeAmenities`, `Bookings`, `Payments`, `Reviews`, `ContactEnquiries`

Seed data: 1 hotel profile, 8 amenities, 6 room types, 8 rooms, 26 room-type-amenity mappings.

## Setup

1. Prerequisites: .NET 8 SDK, SQL Server Express.
2. Clone the repository.
3. Check the connection string in `appsettings.json`: `Server=.\SQLEXPRESS;Database=HotelBookingDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True`
4. `dotnet ef database update`
5. `dotnet run`

## Demo accounts

- Customer: `demo@customer.com` / `Demo@123`
- Admin: register on the Register page, then run in SQL Server: `UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@hotel.com';` then log out and log in again. Demo admin: `admin@hotel.com` / `Admin@123`

## Demo payment

- Card: `4111 1111 1111 1111` (any future expiry, any CVV). UPI: any valid-looking UPI ID.
- The "Simulate failure" checkbox tests the failed-payment path: the booking stays Pending for retry inside the 15-minute hold.

## Project structure

- `Controllers/` — Home, Rooms, Bookings, Payments, Reviews, Account, Admin, AdminRooms, AdminRoomTypes, AdminReviews, AdminEnquiries, Pages
- `Models/` — 10 entities
- `Data/ApplicationDbContext.cs` — DbSets, constraints, seed data
- `Services/PendingBookingExpiryService.cs` — background expiry of Pending bookings
- `Views/` — Razor pages with per-page banners

## Known limitations

- Demo payments only (no real payment gateway).
- No email sending, no PDF/Excel export, no Swagger/API.
