using Microsoft.EntityFrameworkCore;
using ConcertTicketManagementSystem.Models;
namespace ConcertTicketManagementSystem.Models;

using Microsoft.EntityFrameworkCore;


public class ReservationContext : DbContext
{
    public ReservationContext(DbContextOptions<ReservationContext> options)
        : base(options)
    {
    }

    public DbSet<EventItem> ReservationItems { get; set; } = null!;

public DbSet<ConcertTicketManagementSystem.Models.ReservationItem> ReservationItem { get; set; } = default!;
}