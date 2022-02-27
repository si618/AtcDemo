namespace AtcDemo.Client.Data;

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AtcDemo.Shared;
using Google.Protobuf.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Serilog;
using static AtcDemo.Shared.AtcRecordService;

// This service synchronizes the Sqlite DB with both the backend server and the browser's IndexedDb storage
class DataSynchronizer
{
    private static readonly ILogger s_log = Log.ForContext(typeof(DataSynchronizer));

    public const string SqliteDbFilename = "atc.db";
    public const int FetchMaxCount = 500;

    private readonly Task _firstTimeSetupTask;
    private readonly IDbContextFactory<AtcClientDbContext> _dbContextFactory;
    private readonly AtcRecordServiceClient _service;
    private bool _isSynchronizing;

    public DataSynchronizer(
        IJSRuntime js,
        IDbContextFactory<AtcClientDbContext> dbContextFactory,
        AtcRecordServiceClient service)
    {
        _dbContextFactory = dbContextFactory;
        _service = service;
        _firstTimeSetupTask = FirstTimeSetupAsync(js);
    }

    public int SyncCompleted { get; private set; }
    public int SyncTotal { get; private set; }

    public async Task<AtcClientDbContext> GetPreparedDbContextAsync()
    {
        await _firstTimeSetupTask;
        return await _dbContextFactory.CreateDbContextAsync();
    }

    public void SynchronizeInBackground()
    {
        _ = EnsureSynchronizingAsync();
    }

    public event Action? OnUpdate;
    public event Action<Exception>? OnError;

    private async Task FirstTimeSetupAsync(IJSRuntime js)
    {
        var module = await js.InvokeAsync<IJSObjectReference>("import", "./dbstorage.js");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser")))
        {
            await module.InvokeVoidAsync("synchronizeFileWithIndexedDb", SqliteDbFilename);
        }

        using var db = await _dbContextFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

    private async Task EnsureSynchronizingAsync()
    {
        // Don't run multiple syncs in parallel. This simple logic is adequate because of single-threadedness.
        if (_isSynchronizing)
        {
            return;
        }

        try
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _isSynchronizing = true;
            SyncCompleted = 0;
            SyncTotal = 0;

            // Get a DB context
            using var db = await GetPreparedDbContextAsync();
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            // Begin fetching any updates to the dataset from the backend server
            var mostRecentUpdate = db.Chemicals.OrderByDescending(p => p.ModifiedTicks).FirstOrDefault()?.ModifiedTicks;

            var connection = db.Database.GetDbConnection();
            connection.Open();

            while (true)
            {
                var request = new AtcRecordRequest
                {
                    MaxCount = FetchMaxCount,
                    ModifiedSinceTicks = mostRecentUpdate ?? -1
                };
                var response = await _service.GetAtcRecordsAsync(request);
                var syncRemaining = response.ModifiedCount - response.Chemicals.Count;
                SyncCompleted += response.Chemicals.Count;
                SyncTotal = SyncCompleted + syncRemaining;

                if (response.Chemicals.Count == 0)
                {
                    break;
                }
                else
                {
                    mostRecentUpdate = response.Chemicals.Last().ModifiedTicks;
                    // TODO: Bulk insert much faster, but nice to figure out why SaveChanges borks
                    //db.Chemicals.AddRange(response.Chemicals);
                    //db.SaveChanges();
                    BulkInsert(connection, response.Chemicals);

                    s_log.Information("Saved {Count:N0} ATC records in {Elapsed:N0}ms",
                        db.Chemicals.Count(), stopwatch.ElapsedMilliseconds);

                    OnUpdate?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            s_log.Error(ex, "Error while saving");
            OnError?.Invoke(ex);
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    // TODO: Work out how to reuse bulk insert from SeedAtcData on server (in shared project?)
    private static void BulkInsert(DbConnection connection, IEnumerable<AtcChemical> chemicals)
    {
        static DbParameter AddNamedParameter(DbCommand command, string name)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            command.Parameters.Add(parameter);
            return parameter;
        }

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
            modifiedTicks.Value = chemical.ModifiedTicks;
            //command.ExecuteNonQuery();

            foreach (var dose in chemical.Doses)
            {
                command.CommandText = doseSql + doseValues;
                doseId.Value = ++dosePk;
                chemicalId.Value = chemicalPk;
                ddd.Value = dose.DefinedDailyDose;
                route.Value = dose.AdministrationRoute;
                //command.ExecuteNonQuery();
            }
        }
        transaction.Commit();
    }
}
