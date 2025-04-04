using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ConcertTicketManagementSystem.Models
{
    public class VenueItem
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CustomIdName")]
        public long VenueId { get; set; }
        public string? Name { get; set; }
        public int? Capacity { get; set; } = 0;

        // for simplicity's sake we're going to assume each event lasts an entire day
        // each string is expected to be formatted such that it can be converted to a DateOnly object
        // We also assume none of the dates are duplicates
        public List<string> AvailableDates { get; set; } = new List<string>();

        public List<long> AssociatedEventIds { get; set; } = new List<long>();
    }
}
