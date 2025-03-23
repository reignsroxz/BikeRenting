using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeSharingApp.Models
{
    public class Rental
    {
        [Key]
        public int RentalID { get; set; }

        [Required]
        public int BikeID { get; set; }

        [Required]
        public int RenterID { get; set; }

        [Required]
        public DateTime RentalStart { get; set; }

        public DateTime? RentalEnd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        // Navigation properties
        public virtual Bike Bike { get; set; } = null!;
        public virtual User Renter { get; set; } = null!;
    }
}