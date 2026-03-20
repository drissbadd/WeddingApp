using Microsoft.EntityFrameworkCore;
using WeddingApp.Models;

namespace WeddingApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Guest> Guests => Set<Guest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Guest>()
            .HasIndex(g => g.RsvpToken)
            .IsUnique();
    }
}
