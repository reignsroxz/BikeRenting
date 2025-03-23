using BikeSharingApp.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BikeSharingApp.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(BikeSharingDbContext context)
        {
            if (!context.Users.Any())
            {
                // Add Users
                var user1 = new User
                {
                    Username = "Admin",
                    FirstName = "Romel",
                    LastName = "Perera",
                    Email = "Romel@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("3620"),
                    PhoneNumber = "0767594282",
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                };

                var user2 = new User
                {
                    Username = "nadeemnishaam",
                    FirstName = "Nadeem",
                    LastName = "Nishaam",
                    Email = "nadeem@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("1108"),
                    PhoneNumber = "0778064282",
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                };

                context.Users.AddRange(user1, user2);
                await context.SaveChangesAsync();

                // Add Bikes
                var bike1 = new Bike
                {
                    BikeType = "Mountain Bike",
                    Location = "Downtown",
                    Description = "A sturdy mountain bike perfect for trails",
                    IsAvailable = true,
                    HourlyRate = 15.00M,
                    Owner = user1
                };

                var bike2 = new Bike
                {
                    BikeType = "Road Bike",
                    Location = "Uptown",
                    Description = "Lightweight road bike for speed enthusiasts",
                    IsAvailable = true,
                    HourlyRate = 12.50M,
                    Owner = user1
                };

                var bike3 = new Bike
                {
                    BikeType = "City Bike",
                    Location = "Midtown",
                    Description = "Comfortable city bike for casual rides",
                    IsAvailable = true,
                    HourlyRate = 10.00M,
                    Owner = user1
                };

                context.Bikes.AddRange(bike1, bike2, bike3);
                await context.SaveChangesAsync();

                // Add Rentals
                var rental1 = new Rental
                {
                    Bike = bike1,
                    Renter = user2,
                    RentalStart = DateTime.Now.AddDays(-2),
                    RentalEnd = DateTime.Now.AddDays(-1),
                    TotalCost = 180.00M // 12 hours * $15.00
                };

                var rental2 = new Rental
                {
                    Bike = bike3,
                    Renter = user2,
                    RentalStart = DateTime.Now.AddDays(-1),
                    RentalEnd = DateTime.Now.AddDays(-0.5),
                    TotalCost = 120.00M // 12 hours * $10.00
                };

                context.Rentals.AddRange(rental1, rental2);
                await context.SaveChangesAsync();

                // Add Reviews
                var review1 = new Review
                {
                    Bike = bike1,
                    Renter = user2,
                    Rating = 5,
                    ReviewText = "Excellent mountain bike! Perfect for the trails I rode.",
                    ReviewDate = DateTime.Now.AddDays(-1)
                };

                var review2 = new Review
                {
                    Bike = bike3,
                    Renter = user2,
                    Rating = 4,
                    ReviewText = "Very comfortable city bike. Great for sightseeing.",
                    ReviewDate = DateTime.Now.AddDays(-0.5)
                };

                context.Reviews.AddRange(review1, review2);
                await context.SaveChangesAsync();

                // Add Messages
                var message1 = new Message
                {
                    GuestName = "Mike Wilson",
                    GuestEmail = "mike@example.com",
                    MessageText = "Is the mountain bike available next weekend?",
                    MessageDate = DateTime.Now.AddDays(-3),
                    IsRead = true
                };

                var message2 = new Message
                {
                    GuestName = "Sarah Johnson",
                    GuestEmail = "sarah@example.com",
                    MessageText = "Do you have any bikes suitable for beginners?",
                    MessageDate = DateTime.Now.AddDays(-1),
                    IsRead = false
                };

                context.Messages.AddRange(message1, message2);
                await context.SaveChangesAsync();
            }
        }
    }
} 