using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ConcertTicketManagementSystem.Models
{
    public class EventItem
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CustomIdName")]
        public long EventId { get; set; }

        public string? Name { get; set; }

        // This string is expected to be formatted such that it can be converted to a DateOnly object
        public string Date { get; set; } = "";

        public string Description { get; set; } = "";

        // For requests, this can be used to find a venue
        public int Capacity { get; set; }

        // Any value given in http request will be ignored
        public long? AssociatedVenueId { get; set; }

        // The list will be required to include at least one element.
        // Format: a 2d array where each row contains one ticket type, including name, price, total available, and still available
        // It will also be validated that the sum of totalTickets is less than capacity.
        public required string[][] TicketPrices { get; set; }

        //public List<ticketItem> tickets { get; set; };
    }
}
