namespace AtcDemo.Server.Data;

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using AtcDemo.Shared;
using CsvHelper;
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

        SeedAtcLevels(db);
        SeedAtcClassifications(db);
    }

    static void SeedAtcClassifications(AtcDbContext db)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var dir = Path.GetDirectoryName(typeof(SeedAtcData).Assembly.Location)!;
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

    static void SeedAtcLevels(AtcDbContext db)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var dir = Path.GetDirectoryName(typeof(SeedAtcData).Assembly.Location)!;
        var file = Path.Combine(dir, "Data", "ATC-2021AB.csv")!;
        if (!File.Exists(file))
        {
            throw new FileNotFoundException("File not found", file);
        }

        using var fileReader = (TextReader)File.OpenText(file);
        using var csv = new CsvReader(fileReader, CultureInfo.InvariantCulture);
        var levels = new List<AtcLevel>();
        var count = 0;
        /*
         0:  Class ID,
         1:  Preferred Label,
         2:  Synonyms,
         3:  Definitions,
         4:  Obsolete,
         5:  CUI,
         6:  Semantic Types,
         7:  Parents,
         8:  ATC LEVEL,
         9:  Is Drug Class,
         10: Semantic type UMLS property
         */
        while (csv.Read())
        {
            if (count++ == 0)
            {
                continue; // Skip headings
            }

            var classId = csv.GetField<string>(0);
            if (classId.StartsWith("ATC/"))
            {
                classId = classId[4..];
            }
            else
            {
                // TODO: Worth attempting STY?
                continue;
            }

            var preferredLabel = csv.GetField<string>(1);
            var atcLevel = csv.GetField<int?>(8);
            var isDrugClass = csv.GetField<string>(9);

            if (atcLevel is null || atcLevel == 5 || isDrugClass != "Y")
            {
                // TODO: Work out integration with ATC.json and its DDD?
                continue;
            }

            levels.Add(new AtcLevel
            {
                Id = count++,
                Level = atcLevel!.Value,
                Code = classId,
                Name = preferredLabel
            });
        }
        levels = levels.OrderBy(l => l.Code).ToList();
        var config = new BulkConfig() { PreserveInsertOrder = true };
        db.BulkInsert(levels, config);
        db.SaveChanges();

        s_log.Information("Seeded {Count:N0} ATC levels in {Elapsed:N0}ms",
            db.Levels.Count(), stopwatch.ElapsedMilliseconds);
    }
}
