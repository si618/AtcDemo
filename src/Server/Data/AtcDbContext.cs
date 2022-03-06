namespace AtcDemo.Server.Data;

using AtcDemo.Shared;
using Microsoft.EntityFrameworkCore;

public class AtcDbContext : DbContext
{
    public AtcDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AtcClassification>().HasKey(c => c.Id);
        builder.Entity<AtcDose>().HasKey(d => d.Id);
        builder.Entity<AtcLevel>().HasKey(l => l.Id);

        builder.Entity<AtcClassification>()
            .HasMany(c => c.Doses)
            .WithOne()
            .HasForeignKey(d => d.ClassificationId);

        builder.Entity<AtcClassification>().HasIndex(
            nameof(AtcClassification.Code),
            nameof(AtcClassification.Name),
            nameof(AtcClassification.ModifiedTicks));
    }

    public DbSet<AtcClassification> Classifications { get; set; } = default!;

    public DbSet<AtcDose> Doses { get; set; } = default!;

    public DbSet<AtcLevel> Levels { get; set; } = default!;
}
