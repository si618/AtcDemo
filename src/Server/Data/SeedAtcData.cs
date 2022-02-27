namespace AtcDemo.Server.Data;

using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using AtcDemo.Shared;
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
        //db.Chemicals.AddRange(chemicals);
        //db.SaveChanges();
        var connection = db.Database.GetDbConnection();
        connection.Open();
        BulkInsert(connection, chemicals);

        s_log.Information("Seeded {Count:N0} ATC records in {Elapsed:N0}ms",
            db.Chemicals.Count(), stopwatch.ElapsedMilliseconds);
    }

    private static void BulkInsert(DbConnection connection, IEnumerable<AtcChemical> chemicals)
    {
        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();
        var chemicalId = AddNamedParameter(command, "$ChemicalId");
        var code = AddNamedParameter(command, "$Code");
        var name = AddNamedParameter(command, "$Name");
        var level1 = AddNamedParameter(command, "$Level1");
        var level2 = AddNamedParameter(command, "$Level2");
        var level3 = AddNamedParameter(command, "$Level3");
        var level4 = AddNamedParameter(command, "$Level4");
        var level5 = AddNamedParameter(command, "$Level5");
        var modifiedTicks = AddNamedParameter(command, "$ModifiedTicks");
        var doseId = AddNamedParameter(command, "$DoseId");
        var ddd = AddNamedParameter(command, "$DDD");
        var route = AddNamedParameter(command, "$Route");

        var chemicalSql =
            "INSERT OR REPLACE INTO Chemicals (Id, Code, Name, Level1AnatomicalMainGroup, " +
            "Level2TherapeuticSubgroup, Level3PharmacologicalSubgroup, Level4ChemicalSubgroup, " +
            "Level5ChemicalSubstance, ModifiedTicks) ";
        var doseSql =
            "INSERT OR REPLACE INTO Doses (Id, ChemicalId, DefinedDailyDose, AdministrationRoute) ";
        var chemicalValues =
            $"VALUES ({chemicalId.ParameterName}, {code.ParameterName}, {name.ParameterName}, " +
            $"{level1.ParameterName}, {level2.ParameterName}, {level3.ParameterName}, " +
            $"{level4.ParameterName}, {level5.ParameterName}, {modifiedTicks.ParameterName})";
        var doseValues =
            $"VALUES ({doseId.ParameterName}, {chemicalId.ParameterName}, {ddd.ParameterName}, " +
            $"{route.ParameterName})";

        var chemicalPk = 0;
        var dosePk = 0;
        var ticks = DateTime.UtcNow.Ticks;

        foreach (var chemical in chemicals)
        {
            command.CommandText = chemicalSql + chemicalValues;
            chemicalId.Value = ++chemicalPk;
            code.Value = chemical.Code;
            name.Value = chemical.Name;
            level1.Value = chemical.Level1AnatomicalMainGroup;
            level2.Value = chemical.Level2TherapeuticSubgroup;
            level3.Value = chemical.Level3PharmacologicalSubgroup;
            level4.Value = chemical.Level4ChemicalSubgroup;
            level5.Value = chemical.Level5ChemicalSubstance;
            modifiedTicks.Value = ticks;
            command.ExecuteNonQuery();

            foreach (var dose in chemical.Doses)
            {
                command.CommandText = doseSql + doseValues;
                doseId.Value = ++dosePk;
                chemicalId.Value = chemicalPk;
                ddd.Value = dose.DefinedDailyDose;
                route.Value = dose.AdministrationRoute;
                command.ExecuteNonQuery();
            }
        }
        transaction.Commit();

        static DbParameter AddNamedParameter(DbCommand command, string name)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}
