using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Models;

namespace SimplyFly.Api.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(AppDbContext context)
        {
            // Ensure the database is created
            await context.Database.EnsureCreatedAsync();

            // Check if we already have users
            if (await context.Users.AnyAsync())
            {
                return; // DB has been seeded
            }

            // Create password hasher
            var hasher = new PasswordHasher<User>();

            // Create admin user
            var adminUser = new User
            {
                Name = "Admin User",
                Email = "admin@example.com",
                Role = "Admin"
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "password123");

            // Create regular user
            var regularUser = new User
            {
                Name = "Test User",
                Email = "user@example.com", 
                Role = "User"
            };
            regularUser.PasswordHash = hasher.HashPassword(regularUser, "password123");

            context.Users.AddRange(adminUser, regularUser);
            await context.SaveChangesAsync();

            // Add sample flights (only with the simplified schema: Id, Origin, Destination, Price)
            var flights = new List<Flight>
            {
                new Flight { Origin = "NYC", Destination = "LAX", Price = 299.99m },
                new Flight { Origin = "LAX", Destination = "CHI", Price = 199.99m },
                new Flight { Origin = "CHI", Destination = "MIA", Price = 249.99m },
                new Flight { Origin = "NYC", Destination = "MIA", Price = 279.99m },
                new Flight { Origin = "LAX", Destination = "NYC", Price = 319.99m }
            };

            context.Flights.AddRange(flights);
            await context.SaveChangesAsync();
        }
    }
}