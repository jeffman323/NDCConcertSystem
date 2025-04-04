namespace ConcertTicketManagementSystem.Models;

using Microsoft.EntityFrameworkCore;


public class EventContext : DbContext
{
    public EventContext(DbContextOptions<EventContext> options)
        : base(options)
    {
    }

    public DbSet<EventItem> EventItems { get; set; } = null!;
}