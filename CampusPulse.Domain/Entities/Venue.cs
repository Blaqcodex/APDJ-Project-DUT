namespace CampusPulse.Domain.Entities
{
    public class Venue
    {
        public int VenueId { get; set; }

        public string VenueName { get; set; } = "";

        public string Campus { get; set; } = "";
    }
}