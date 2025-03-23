using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeSharingApp.Models
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        [Required]
        [StringLength(100)]
        public string GuestName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string GuestEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string MessageText { get; set; } = string.Empty;

        [Required]
        public DateTime MessageDate { get; set; }

        public bool IsRead { get; set; }
    }
}