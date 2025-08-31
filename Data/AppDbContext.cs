using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Models;

namespace SimplyFly.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PassengerDetail> PassengerDetails { get; set; }
        public DbSet<FlightOwner> FlightOwners { get; set; }
        public DbSet<FlightDetail> FlightDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Configure Flight entity
            modelBuilder.Entity<Flight>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Origin).IsRequired().HasMaxLength(10);
                entity.Property(f => f.Destination).IsRequired().HasMaxLength(10);
                entity.Property(f => f.Price).HasColumnType("decimal(10,2)");
            });

            // Configure Booking entity
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.BookingId);
                entity.HasOne(b => b.User)
                      .WithMany()
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(b => b.FlightDetails)
                      .WithMany()
                      .HasForeignKey(b => b.FlightId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Review entity
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Flight)
                    .WithMany()
                    .HasForeignKey(r => r.FlightId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(r => r.Rating).IsRequired();
                entity.Property(r => r.Comment).HasMaxLength(1000);
                entity.Property(r => r.SubmittedAt).IsRequired(); // Changed from CreatedAt to SubmittedAt
            });

            // Configure PassengerDetail entity
            modelBuilder.Entity<PassengerDetail>(entity =>
            {
                entity.HasKey(p => p.PassengerId);
                entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Age).IsRequired();
                entity.Property(p => p.Gender).IsRequired().HasMaxLength(10);
                entity.Property(p => p.Nationality).IsRequired().HasMaxLength(100);
                entity.Property(p => p.SeatNo).HasMaxLength(10);
                entity.Property(p => p.PassportNumber).HasMaxLength(50);
                entity.HasOne(p => p.User)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Booking)
                    .WithMany()
                    .HasForeignKey(p => p.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure FlightOwner entity
            modelBuilder.Entity<FlightOwner>(entity =>
            {
                entity.HasKey(fo => fo.FlightOwnerId);
                entity.Property(fo => fo.AirlineName).IsRequired().HasMaxLength(100);
                entity.HasOne(fo => fo.User)
                    .WithMany()
                    .HasForeignKey(fo => fo.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(fo => fo.UserId).IsUnique();
            });

            // Configure FlightDetail entity
            modelBuilder.Entity<FlightDetail>(entity =>
            {
                entity.HasKey(fd => fd.FlightDetailId);
                entity.Property(fd => fd.FlightName).IsRequired().HasMaxLength(100);
                entity.Property(fd => fd.BaggageInfo).HasMaxLength(200);
                entity.Property(fd => fd.NumberOfSeats).IsRequired();
                entity.Property(fd => fd.Fare).HasColumnType("decimal(10,2)");
                entity.HasOne(fd => fd.Flight)
                    .WithMany()
                    .HasForeignKey(fd => fd.FlightId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(fd => fd.FlightOwner)
                    .WithMany(fo => fo.FlightDetails)
                    .HasForeignKey(fd => fd.FlightOwnerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}