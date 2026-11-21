using HotelBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<HotelProfile> HotelProfiles { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<RoomType> RoomTypes { get; set; }

    public DbSet<Room> Rooms { get; set; }

    public DbSet<Amenity> Amenities { get; set; }

    public DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }

    public DbSet<Booking> Bookings { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<ContactEnquiry> ContactEnquiries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // HotelProfile: single-row table, Id must always be 1.
        modelBuilder.Entity<HotelProfile>()
            .ToTable(t => t.HasCheckConstraint("CK_HotelProfile_SingleRow", "HotelProfileId = 1"));

        // User: Email must be unique.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // RoomType: Name must be unique.
        modelBuilder.Entity<RoomType>()
            .HasIndex(rt => rt.Name)
            .IsUnique();

        // Room: RoomNumber must be unique. Status is operational only.
        modelBuilder.Entity<Room>()
            .HasIndex(r => r.RoomNumber)
            .IsUnique();

        modelBuilder.Entity<Room>()
            .ToTable(t => t.HasCheckConstraint("CK_Room_Status", "Status IN ('Available', 'Maintenance', 'OutOfService')"));

        modelBuilder.Entity<Room>()
            .HasOne(r => r.RoomType)
            .WithMany(rt => rt.Rooms)
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // RoomTypeAmenity: many-to-many junction between RoomType and Amenity.
        modelBuilder.Entity<RoomTypeAmenity>()
            .HasOne(rta => rta.RoomType)
            .WithMany()
            .HasForeignKey(rta => rta.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoomTypeAmenity>()
            .HasOne(rta => rta.Amenity)
            .WithMany(a => a.RoomTypeAmenities)
            .HasForeignKey(rta => rta.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Booking: BookingRef must be unique. Check-out must be after check-in.
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingRef)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .ToTable(t => t.HasCheckConstraint("CK_Booking_CheckOutAfterCheckIn", "CheckOutDate > CheckInDate"));

        modelBuilder.Entity<Booking>()
            .ToTable(t => t.HasCheckConstraint("CK_Booking_Status", "Status IN ('Pending', 'Confirmed', 'Cancelled', 'CheckedIn', 'CheckedOut', 'NoShow')"));

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment: one booking can have many attempts. AttemptNo is unique per booking.
        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.BookingId, p.AttemptNo })
            .IsUnique();

        // At most one Success payment per booking.
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.BookingId)
            .IsUnique()
            .HasFilter("[Status] = 'Success'")
            .HasDatabaseName("UX_Payments_OneSuccessPerBooking");

        modelBuilder.Entity<Payment>()
            .ToTable(t => t.HasCheckConstraint("CK_Payment_Status", "Status IN ('Pending', 'Success', 'Failed', 'Refunded')"));

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review: one booking can have at most one review.
        modelBuilder.Entity<Review>()
            .HasIndex(r => r.BookingId)
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.RoomType)
            .WithMany()
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data: HotelProfile (single row, Id = 1).
        modelBuilder.Entity<HotelProfile>().HasData(
            new HotelProfile
            {
                HotelProfileId = 1,
                HotelName = "Fernwood Heritage Retreat",
                Tagline = "A quiet heritage stay above the valley",
                Address = "Fernwood Estate, Jharipani Road, Mussoorie Hills",
                City = "Mussoorie",
                State = "Uttarakhand",
                Phone = "+91 98765 43210",
                Email = "stay@fernwoodheritage.in",
                Description = "A quiet heritage stay above the valley. Old stone walls, wooden ceilings, valley views, homely food and bonfire evenings.",
                HeroImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=1920&q=80",
                CheckInTime = new TimeOnly(14, 0),
                CheckOutTime = new TimeOnly(11, 0)
            });

        // Seed data: Amenities.
        modelBuilder.Entity<Amenity>().HasData(
            new Amenity { AmenityId = 1, Name = "Free Wi-Fi", Icon = "wifi", Description = "Free high-speed Wi-Fi in rooms and common areas." },
            new Amenity { AmenityId = 2, Name = "Valley View", Icon = "view", Description = "Rooms with direct valley views." },
            new Amenity { AmenityId = 3, Name = "Private Terrace", Icon = "terrace", Description = "Private sit-out terrace with hill breeze." },
            new Amenity { AmenityId = 4, Name = "Jacuzzi", Icon = "jacuzzi", Description = "Private jacuzzi in the premium suite." },
            new Amenity { AmenityId = 5, Name = "In-Room Dining", Icon = "dining", Description = "Food served in your room on request." },
            new Amenity { AmenityId = 6, Name = "Free Parking", Icon = "parking", Description = "Free secure parking inside the premises." },
            new Amenity { AmenityId = 7, Name = "Bonfire", Icon = "bonfire", Description = "Evening bonfire on request, weather permitting." },
            new Amenity { AmenityId = 8, Name = "Daily Housekeeping", Icon = "cleaning", Description = "Daily room cleaning and fresh linen." });

        // Seed data: RoomTypes (same 6 types as the homepage).
        modelBuilder.Entity<RoomType>().HasData(
            new RoomType { RoomTypeId = 1, Name = "Semi Deluxe Room", Description = "Cozy double room with garden view, ideal for couples on a short stay.", PricePerNight = 2800, SizeSqFt = 140, BedType = "Double Bed", ViewType = "Garden View", MaxAdults = 2, MaxChildren = 0, MaxGuests = 2, ImageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?w=800&q=80", IsActive = true },
            new RoomType { RoomTypeId = 2, Name = "Log Hut", Description = "Rustic wooden hut with a private terrace and hill breeze.", PricePerNight = 3200, SizeSqFt = 210, BedType = "Double Bed", ViewType = "Terrace", MaxAdults = 2, MaxChildren = 0, MaxGuests = 2, ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d57733427?w=800&q=80", IsActive = true },
            new RoomType { RoomTypeId = 3, Name = "Deluxe Room", Description = "Spacious deluxe room with full valley view and a cozy sit-out.", PricePerNight = 4500, SizeSqFt = 380, BedType = "Double Bed", ViewType = "Valley View", MaxAdults = 2, MaxChildren = 1, MaxGuests = 3, ImageUrl = "https://images.unsplash.com/photo-1582719508461-905c673771fd?w=800&q=80", IsActive = true },
            new RoomType { RoomTypeId = 4, Name = "Suite Room", Description = "Large suite with separate sitting area and valley view.", PricePerNight = 5200, SizeSqFt = 425, BedType = "Double Bed", ViewType = "Valley View", MaxAdults = 2, MaxChildren = 1, MaxGuests = 3, ImageUrl = "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?w=800&q=80", IsActive = true },
            new RoomType { RoomTypeId = 5, Name = "Family Suite", Description = "Family suite with a double bed and a sofa bed, best for families.", PricePerNight = 6800, SizeSqFt = 550, BedType = "Double + Sofa Bed", ViewType = "Valley View", MaxAdults = 3, MaxChildren = 2, MaxGuests = 4, ImageUrl = "https://images.unsplash.com/photo-1591088398332-8a7791972843?w=800&q=80", IsActive = true },
            new RoomType { RoomTypeId = 6, Name = "Fern Suite with Jacuzzi", Description = "Our premium suite with a private jacuzzi and full glass view of the valley.", PricePerNight = 12500, SizeSqFt = 650, BedType = "King Bed", ViewType = "Valley View", MaxAdults = 2, MaxChildren = 0, MaxGuests = 2, ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=800&q=80", IsActive = true });

        // Seed data: Rooms (8 rooms in the single hotel).
        modelBuilder.Entity<Room>().HasData(
            new Room { RoomId = 1, RoomNumber = "101", RoomTypeId = 1, Status = "Available" },
            new Room { RoomId = 2, RoomNumber = "102", RoomTypeId = 1, Status = "Available" },
            new Room { RoomId = 3, RoomNumber = "201", RoomTypeId = 2, Status = "Available" },
            new Room { RoomId = 4, RoomNumber = "301", RoomTypeId = 3, Status = "Available" },
            new Room { RoomId = 5, RoomNumber = "302", RoomTypeId = 3, Status = "Available" },
            new Room { RoomId = 6, RoomNumber = "401", RoomTypeId = 4, Status = "Available" },
            new Room { RoomId = 7, RoomNumber = "501", RoomTypeId = 5, Status = "Available" },
            new Room { RoomId = 8, RoomNumber = "601", RoomTypeId = 6, Status = "Available" });

        // Seed data: RoomTypeAmenity mappings.
        modelBuilder.Entity<RoomTypeAmenity>().HasData(
            new RoomTypeAmenity { RoomTypeId = 1, AmenityId = 1 },
            new RoomTypeAmenity { RoomTypeId = 1, AmenityId = 5 },
            new RoomTypeAmenity { RoomTypeId = 1, AmenityId = 8 },
            new RoomTypeAmenity { RoomTypeId = 2, AmenityId = 1 },
            new RoomTypeAmenity { RoomTypeId = 2, AmenityId = 3 },
            new RoomTypeAmenity { RoomTypeId = 2, AmenityId = 5 },
            new RoomTypeAmenity { RoomTypeId = 2, AmenityId = 8 },
            new RoomTypeAmenity { RoomTypeId = 3, AmenityId = 1 },
            new RoomTypeAmenity { RoomTypeId = 3, AmenityId = 2 },
            new RoomTypeAmenity { RoomTypeId = 3, AmenityId = 5 },
            new RoomTypeAmenity { RoomTypeId = 3, AmenityId = 8 },
            new RoomTypeAmenity { RoomTypeId = 4, AmenityId = 1 },
            new RoomTypeAmenity { RoomTypeId = 4, AmenityId = 2 },
            new RoomTypeAmenity { RoomTypeId = 4, AmenityId = 5 },
            new RoomTypeAmenity { RoomTypeId = 4, AmenityId = 8 },
            new RoomTypeAmenity { RoomTypeId = 5, AmenityId = 1 },
            new RoomTypeAmenity { RoomTypeId = 5, AmenityId = 2 },
            new RoomTypeAmenity { RoomTypeId = 5, AmenityId = 5 },
            new RoomTypeAmenity { RoomTypeId = 5, AmenityId = 8 },
            new RoomTypeAmenity { RoomTypeId = 6, AmenityId = 1 },
            new RoomTypeAmenity { RoomTypeId = 6, AmenityId = 2 },
            new RoomTypeAmenity { RoomTypeId = 6, AmenityId = 3 },
            new RoomTypeAmenity { RoomTypeId = 6, AmenityId = 4 },
            new RoomTypeAmenity { RoomTypeId = 6, AmenityId = 5 },
            new RoomTypeAmenity { RoomTypeId = 6, AmenityId = 7 },
            new RoomTypeAmenity { RoomTypeId = 6, AmenityId = 8 });
    }
}
