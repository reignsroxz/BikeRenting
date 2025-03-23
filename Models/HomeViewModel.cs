namespace BikeSharingApp.Models
{
    public class HomeViewModel
    {
        public int AvailableBikesCount { get; set; }
        public int TotalUsersCount { get; set; }
        public List<Review> RecentReviews { get; set; } = new List<Review>();
    }
} 