using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeSharingApp.Models
{
    public class Bike
    {
        [Key]
        public int BikeID { get; set; }

        [Required]
        public int OwnerID { get; set; }

        [Required]
        [StringLength(50)]
        public string BikeType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public bool IsAvailable { get; set; } = true;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        // Navigation properties
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}