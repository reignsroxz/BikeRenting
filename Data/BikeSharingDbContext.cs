using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BikeSharingApp.Models;

namespace BikeSharingApp.Data
{
    public class BikeSharingDbContext : DbContext
    {
        private readonly ILogger<BikeSharingDbContext> _logger;

        public BikeSharingDbContext(
            DbContextOptions<BikeSharingDbContext> options,
            ILogger<BikeSharingDbContext> logger) : base(options)
        {
            _logger = logger;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Bike> Bikes { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                _logger.LogWarning("DbContext is not configured. Using default configuration.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Bikes)
                .WithOne(b => b.Owner)
                .HasForeignKey(b => b.OwnerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Rentals)
                .WithOne(r => r.Renter)
                .HasForeignKey(r => r.RenterID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reviews)
                .WithOne(r => r.Renter)
                .HasForeignKey(r => r.RenterID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bike>()
                .HasMany(b => b.Rentals)
                .WithOne(r => r.Bike)
                .HasForeignKey(r => r.BikeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bike>()
                .HasMany(b => b.Reviews)
                .WithOne(r => r.Bike)
                .HasForeignKey(r => r.BikeID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}