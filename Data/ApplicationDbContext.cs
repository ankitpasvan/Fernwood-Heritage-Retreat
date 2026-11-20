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
            .HasIndex(p => p.BookingId);

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
    }
}
