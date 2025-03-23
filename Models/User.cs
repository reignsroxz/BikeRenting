using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeSharingApp.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime RegistrationDate { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<Bike> Bikes { get; set; } = new List<Bike>();
        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}