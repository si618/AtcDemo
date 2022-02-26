namespace AtcDemo.Client.Data;

using AtcDemo.Shared;
using Microsoft.EntityFrameworkCore;

internal class AtcClientDbContext : DbContext
{
    public AtcClientDbContext(DbContextOptions<AtcClientDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AtcChemical>().HasKey(p => p.Id);
        builder.Entity<AtcDose>().HasKey(p => p.Id);

        builder.Entity<AtcChemical>()
            .HasMany(c => c.Doses)
            .WithOne()
            .HasForeignKey(d => d.ChemicalId);

        builder.Entity<AtcChemical>().HasIndex(
            nameof(AtcChemical.Code),
            nameof(AtcChemical.Name),
            nameof(AtcChemical.ModifiedTicks));
    }

    public DbSet<AtcChemical> Chemicals { get; set; } = default!;

    public DbSet<AtcDose> Doses { get; set; } = default!;
}
