
namespace ConcertTicketManagementSystem.Models
{
    public class VenueItemDTO
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public int? Capacity { get; set; } = 0;

        // for simplicity's sake we're going to assume each event lasts an entire day
        // each string is expected to be formatted such that it can be converted to a DateOnly object
        // We also assume none of the dates are duplicates
        public List<string> AvailableDates { get; set; } = new List<string>();

    }
}
