using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ConcertTicketManagementSystem.Models
{
    public class ReservationItem
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CustomIdName")]
        public long reservationId { get; set; }
        public long eventID { get; set; }

        public int ticketType { get; set; }

        public DateTime reservationEnds { get; set; }

        public string reservingUser { get; set; } = "";
    }
}
