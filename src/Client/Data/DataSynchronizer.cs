namespace AtcDemo.Client.Data;

using System.Diagnostics;
using System.Runtime.InteropServices;
using AtcDemo.Shared;
using EFCore.BulkExtensions;
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
                    var config = new BulkConfig() { PreserveInsertOrder = true };
                    db.BulkInsertOrUpdate(response.Chemicals.ToList(), config);
                    db.SaveChanges();

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

    // TODO: Workaround for SQLite error on browser
    public async Task<AtcChemical[]> GetAtcRecords()
    {
        var records = new List<AtcChemical>();
        try
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            SyncCompleted = 0;
            SyncTotal = 0;

            var request = new AtcRecordRequest
            {
                MaxCount = 10_000,
                ModifiedSinceTicks = -1
            };
            var response = await _service.GetAtcRecordsAsync(request);
            var syncRemaining = response.ModifiedCount - response.Chemicals.Count;
            SyncCompleted += response.Chemicals.Count;
            SyncTotal = SyncCompleted + syncRemaining;

            records.AddRange(response.Chemicals);
            s_log.Information("gRPC call featch {Count:N0} ATC records in {Elapsed:N0}ms",
                records.Count, stopwatch.ElapsedMilliseconds);

            OnUpdate?.Invoke();
        }
        catch (Exception ex)
        {
            s_log.Error(ex, "Error while fetching data");
            OnError?.Invoke(ex);
        }
        return records.OrderBy(c => c.Code).ToArray();
    }
}
