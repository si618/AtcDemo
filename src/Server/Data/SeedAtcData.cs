namespace AtcDemo.Server.Data;

using System.Diagnostics;
using System.Text.Json;
using AtcDemo.Shared;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

public static class SeedAtcData
{
    private static readonly ILogger s_log = Log.ForContext(typeof(SeedAtcData));

    public static void Seed(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IServiceScopeFactory>();
        using var scope = factory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AtcDbContext>();

        if (!db.Database.EnsureCreated())
        {
            // Don't create database if it already exists
            return;
        }

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var dir = Path.GetDirectoryName(typeof(SeedAtcData).Assembly.Location)!;
        var file = Path.Combine(dir, "Data", "Atc.json")!;
        if (!File.Exists(file))
        {
            throw new FileNotFoundException("File not found", file);
        }
        var json = File.ReadAllText(file);
        var records = JsonSerializer.Deserialize<IEnumerable<Atc.Chemical>>(json)!;
        var chemicals = records.Select(chemical => chemical.ConvertFromRecord());
        var config = new BulkConfig() { PreserveInsertOrder = true };
        db.BulkInsert(chemicals.ToList(), config);
        db.SaveChanges();

        s_log.Information("Seeded {Count:N0} ATC records in {Elapsed:N0}ms",
            db.Chemicals.Count(), stopwatch.ElapsedMilliseconds);
    }
}
