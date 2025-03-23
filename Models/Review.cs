using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeSharingApp.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int BikeID { get; set; }

        [Required]
        public int RenterID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string ReviewText { get; set; } = string.Empty;

        [Required]
        public DateTime ReviewDate { get; set; }

        // Navigation properties
        public virtual Bike? Bike { get; set; }
        public virtual User? Renter { get; set; }
    }
}