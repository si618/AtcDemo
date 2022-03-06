namespace AtcDemo.Server.Data;

using System.Diagnostics;
using System.Text.Json;
using AtcDemo.Shared;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

public static class SeedAtcClassifications
{
    private static readonly ILogger s_log = Log.ForContext(typeof(SeedAtcClassifications));

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

        var dir = Path.GetDirectoryName(typeof(SeedAtcClassifications).Assembly.Location)!;
        var file = Path.Combine(dir, "Data", "ATC.json")!;
        if (!File.Exists(file))
        {
            throw new FileNotFoundException("File not found", file);
        }
        var json = File.ReadAllText(file);
        var records = JsonSerializer.Deserialize<IEnumerable<Atc.Classification>>(json)!;
        var classifications = records.Select(classification => classification.ConvertFromRecord());
        var config = new BulkConfig() { PreserveInsertOrder = true };
        db.BulkInsert(classifications.ToList(), config);
        db.SaveChanges();

        s_log.Information("Seeded {Count:N0} ATC classifications in {Elapsed:N0}ms",
            db.Classifications.Count(), stopwatch.ElapsedMilliseconds);
    }
}
