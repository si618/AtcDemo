namespace AtcDemo.Client.Data;

using System.Diagnostics;
using System.Runtime.InteropServices;
using AtcDemo.Shared;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Serilog;

// This service synchronizes the Sqlite DB with both the backend server and the browser's IndexedDb storage
class DataSynchronizer
{
    private static readonly ILogger s_log = Log.ForContext(typeof(DataSynchronizer));

    public const string SqliteDbFilename = ":memory:"; //"ATC.db";
    public const int FetchMaxCount = 1_000;

    private readonly Task _firstTimeSetupTask;
    private readonly IDbContextFactory<AtcClientDbContext> _dbContextFactory;
    private readonly AtcClassificationRpcService.AtcClassificationRpcServiceClient _service;
    private bool _isSynchronizing;

    public DataSynchronizer(
        IJSRuntime js,
        IDbContextFactory<AtcClientDbContext> dbContextFactory,
        AtcClassificationRpcService.AtcClassificationRpcServiceClient service)
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
            var mostRecentUpdate = db.Classifications.OrderByDescending(p => p.ModifiedTicks).FirstOrDefault()?.ModifiedTicks;

            var connection = db.Database.GetDbConnection();
            connection.Open();

            while (true)
            {
                var request = new AtcClassificationRequest
                {
                    MaxCount = FetchMaxCount,
                    ModifiedSinceTicks = mostRecentUpdate ?? -1
                };
                var response = await _service.GetAtcClassificationsAsync(request);
                var syncRemaining = response.ModifiedCount - response.Classifications.Count;
                SyncCompleted += response.Classifications.Count;
                SyncTotal = SyncCompleted + syncRemaining;

                if (response.Classifications.Count == 0)
                {
                    break;
                }
                else
                {
                    mostRecentUpdate = response.Classifications.Last().ModifiedTicks;
                    var config = new BulkConfig() { PreserveInsertOrder = true };
                    db.BulkInsertOrUpdate(response.Classifications.ToList(), config);
                    db.SaveChanges();

                    s_log.Information("Saved {Count:N0} ATC records in {Elapsed:N0}ms",
                        db.Classifications.Count(), stopwatch.ElapsedMilliseconds);

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
    public async Task<AtcClassification[]> GetAtcClassications()
    {
        var records = new List<AtcClassification>();
        try
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            SyncCompleted = 0;
            SyncTotal = 0;

            var request = new AtcClassificationRequest
            {
                MaxCount = 10_000,
                ModifiedSinceTicks = -1
            };
            var response = await _service.GetAtcClassificationsAsync(request);
            var syncRemaining = response.ModifiedCount - response.Classifications.Count;
            SyncCompleted += response.Classifications.Count;
            SyncTotal = SyncCompleted + syncRemaining;

            records.AddRange(response.Classifications);
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
