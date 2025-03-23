namespace BikeSharingApp.Models
{
    public class BikeIndexViewModel
    {
        public List<Bike> Bikes { get; set; } = new List<Bike>();
        public List<string> BikeTypes { get; set; } = new List<string>();
        public List<string> Locations { get; set; } = new List<string>();
        public string? SearchString { get; set; }
        public string? SelectedBikeType { get; set; }
        public string? SelectedLocation { get; set; }
    }
} 