using SQLite;
using Reflex.Models;

namespace Reflex.Data;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    // Population-derived seed values used before any personal history exists.
    // BestValue for "Lower" tests = typical rested value (not the worst threshold,
    // which would cause division-by-zero in the scoring formula on first use).
    // BestValue for "Higher" tests = typical rested value.
    private static readonly Dictionary<string, (double BestSeed, double WorstThreshold, string Direction)> TestConfig = new()
    {
        { "Stillness", (0.020, 0.120, "Lower")  },  // g RMS
        { "Strike",    (250.0, 650.0, "Lower")   },  // ms composite
        { "Aim",       (0.20,  1.00,  "Lower")   },  // normalized error
        { "Chase",     (0.20,  1.00,  "Lower")   },  // dual-task composite
        { "Pulse",     (40.0,  8.0,   "Higher")  },  // HRV RMSSD ms
    };

    public async Task InitAsync()
    {
        if (_db is not null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "reflex.db3");
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<Session>();
        await _db.CreateTableAsync<TestResult>();
        await _db.CreateTableAsync<Baseline>();

        await SeedBaselinesAsync();
    }

    private async Task SeedBaselinesAsync()
    {
        var count = await _db!.Table<Baseline>().CountAsync();
        if (count >= 5) return;

        var seeds = TestConfig.Select(kvp => new Baseline
        {
            TestName     = kvp.Key,
            BestValue    = kvp.Value.BestSeed,
            Direction    = kvp.Value.Direction,
            UpdatedAt    = DateTime.UtcNow,
            SessionCount = 0
        });

        await _db.InsertAllAsync(seeds);
    }

    // Inserts a completed session and its 5 test results, then refreshes baselines.
    public async Task SaveSessionAsync(Session session, List<TestResult> results)
    {
        session.CreatedAt = DateTime.UtcNow;
        await _db!.InsertAsync(session);

        foreach (var result in results)
        {
            result.SessionId = session.Id;
            await _db.InsertAsync(result);
        }

        await UpdateBaselinesAsync(results);
    }

    private async Task UpdateBaselinesAsync(List<TestResult> results)
    {
        foreach (var result in results)
        {
            var baseline = await _db!.GetAsync<Baseline>(result.TestName);

            bool improved = baseline.Direction == "Lower"
                ? result.PrimaryMetric < baseline.BestValue
                : result.PrimaryMetric > baseline.BestValue;

            if (improved || baseline.SessionCount == 0)
                baseline.BestValue = result.PrimaryMetric;

            baseline.SessionCount++;
            baseline.UpdatedAt = DateTime.UtcNow;
            await _db.UpdateAsync(baseline);
        }
    }

    // Returns the most recent `count` sessions, newest first.
    public async Task<List<Session>> GetRecentSessionsAsync(int count = 7)
        => await _db!.Table<Session>()
                     .OrderByDescending(s => s.CreatedAt)
                     .Take(count)
                     .ToListAsync();

    // Returns all 5 TestResult rows for a given session.
    public async Task<List<TestResult>> GetResultsForSessionAsync(int sessionId)
        => await _db!.Table<TestResult>()
                     .Where(r => r.SessionId == sessionId)
                     .ToListAsync();

    // Returns the single most recent session, or null on first launch.
    public async Task<Session?> GetLastSessionAsync()
        => await _db!.Table<Session>()
                     .OrderByDescending(s => s.CreatedAt)
                     .FirstOrDefaultAsync();

    // Returns all 5 baseline rows for use in the scoring engine.
    public async Task<List<Baseline>> GetBaselinesAsync()
        => await _db!.Table<Baseline>().ToListAsync();

    // Exposes the worst threshold for a test so the scoring engine can use it.
    public static double GetWorstThreshold(string testName)
        => TestConfig.TryGetValue(testName, out var cfg) ? cfg.WorstThreshold : 1.0;
}
