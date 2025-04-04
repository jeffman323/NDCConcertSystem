namespace ConcertTicketManagementSystem.Models;

using Microsoft.EntityFrameworkCore;


public class VenueContext : DbContext
{
    public VenueContext(DbContextOptions<VenueContext> options)
        : base(options)
    {
    }

    public DbSet<VenueItem> VenueItems { get; set; } = null!;
}