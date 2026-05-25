using Reflex.Models;

namespace Reflex.Services;

/// <summary>
/// Translates raw SessionData metrics into 0–100 per-dimension scores,
/// a weighted composite (0–100), and narrative labels.
///
/// Weights:  Stability 20 %  |  Reaction 25 %  |  Precision 20 %
///           Dual-Task 25 %  |  Cardiac  10 %
/// </summary>
public static class ScoringService
{
    // ── Normalisation helpers ──────────────────────────────────────────────

    // "Lower is better": raw = best → 100, raw = worst → 0
    private static double Norm(double raw, double best, double worst)
        => Math.Clamp(100.0 * (1.0 - (raw - best) / (worst - best)), 0, 100);

    // "Higher is better": raw = best → 100, raw = worst → 0
    private static double NormHigh(double raw, double best, double worst)
        => Math.Clamp(100.0 * (raw - worst) / (best - worst), 0, 100);

    // ── Main entry point ───────────────────────────────────────────────────

    public static SessionScore Compute()
    {
        // ── 1. Stability  (20 %) ──────────────────────────────────────────
        // TremorRms  : g RMS  (best 0.020 g, worst 0.120 g)
        // StabilityRms: gyro RMS rad/s (best 0.10, worst 1.00)
        double tremorScore = SessionData.TremorRms > 0
            ? Norm(SessionData.TremorRms, 0.020, 0.120)
            : 50.0;
        double gyroScore = SessionData.StabilityRms > 0
            ? Norm(SessionData.StabilityRms, 0.10, 1.00)
            : tremorScore;
        double stability = 0.60 * tremorScore + 0.40 * gyroScore;

        // ── 2. Reaction  (25 %) ───────────────────────────────────────────
        // MedianRt ms (best 250, worst 650), StdDev ms (best 20, worst 150)
        // LapseCount  (best 0, worst 6)
        double rtScore    = SessionData.MedianRt > 0
            ? Norm(SessionData.MedianRt,   250, 650)
            : 50.0;
        double stdScore   = SessionData.RtStdDev > 0
            ? Norm(SessionData.RtStdDev,    20, 150)
            : 50.0;
        double lapseScore = Norm(SessionData.LapseCount, 0, 6);
        double reaction   = 0.50 * rtScore + 0.30 * stdScore + 0.20 * lapseScore;

        // ── 3. Precision  (20 %) ──────────────────────────────────────────
        // CentroidError: normalised dist/radius (best 0.20, worst 1.00)
        // 999 → skipped / never touched
        double precision = SessionData.CentroidError > 0 && SessionData.CentroidError < 990
            ? Norm(SessionData.CentroidError, 0.20, 1.00)
            : 50.0;

        // ── 4. Dual-Task  (25 %) ──────────────────────────────────────────
        // TrackingError: pixel mean dist → normalised by /180 (best 0.20, worst 1.00)
        // 999 → user never touched the screen
        double dualTask = SessionData.TrackingError < 990
            ? Norm(SessionData.TrackingError / 180.0, 0.20, 1.00)
            : 50.0;

        // ── 5. Cardiac  (10 %) ────────────────────────────────────────────
        // HRV RMSSD ms (best 40, worst 8) — higher is better
        // Heart rate: optimal ≈ 70 BPM; penalises deviation > 40 BPM
        double hrvScore = SessionData.HrvRmssd > 0
            ? NormHigh(SessionData.HrvRmssd, 40.0, 8.0)
            : 50.0;
        double hrDev   = SessionData.HeartRate > 0
            ? Math.Abs(SessionData.HeartRate - 70)
            : 0;
        double hrScore  = Math.Clamp(100.0 * (1.0 - hrDev / 40.0), 0, 100);
        double cardiac  = SessionData.HrvRmssd > 0
            ? 0.60 * hrvScore + 0.40 * hrScore
            : 50.0;

        // ── Weighted composite ────────────────────────────────────────────
        double composite = 0.20 * stability + 0.25 * reaction
                         + 0.20 * precision + 0.25 * dualTask
                         + 0.10 * cardiac;

        return new SessionScore
        {
            Stability = (int)Math.Round(stability),
            Reaction  = (int)Math.Round(reaction),
            Precision = (int)Math.Round(precision),
            DualTask  = (int)Math.Round(dualTask),
            Cardiac   = (int)Math.Round(cardiac),
            Composite = (int)Math.Round(composite)
        };
    }

    // ── Narrative labels ───────────────────────────────────────────────────

    public static string GetBand(int score) => score switch
    {
        >= 85 => "Peak",
        >= 70 => "Good",
        >= 55 => "Caution",
        >= 40 => "Impaired",
        _     => "Critical"
    };

    public static Color GetBandColor(int score) => score switch
    {
        >= 85 => Color.FromArgb("#00E5FF"),
        >= 70 => Color.FromArgb("#44CC88"),
        >= 55 => Color.FromArgb("#FFCC00"),
        >= 40 => Color.FromArgb("#FF8800"),
        _     => Color.FromArgb("#FF4444")
    };

    public static string GetDiagnosis(int score) => score switch
    {
        >= 85 => "Cognitive performance is sharp. Operating at full capacity.",
        >= 70 => "All systems within normal range. Minor fatigue may be present.",
        >= 55 => "Mild signs of fatigue or cognitive load detected.",
        >= 40 => "Several indicators are below typical range. Rest is recommended.",
        _     => "Significant impairment detected. Avoid demanding tasks."
    };

    public static string GetRecommendation(int score) => score switch
    {
        >= 85 => "Excellent time for high-demand, complex tasks.",
        >= 70 => "Suitable for routine and moderate-focus work.",
        >= 55 => "Take a short break before making critical decisions.",
        >= 40 => "Rest, hydrate, and avoid high-stakes activity.",
        _     => "Step away from high-stakes activity now."
    };

    public static string? GetPatternType(SessionScore s)
    {
        if (s.Reaction < 55 && s.Cardiac < 55) return "Fatigue";
        if (s.Stability < 55)                  return "Stress";
        return null;
    }

    // ── Build DB objects ───────────────────────────────────────────────────

    public static (Session session, List<TestResult> results) BuildDbObjects(SessionScore score)
    {
        var session = new Session
        {
            CompositeScore = score.Composite,
            ScoreBand      = GetBand(score.Composite),
            Diagnosis      = GetDiagnosis(score.Composite),
            Recommendation = GetRecommendation(score.Composite),
            PatternType    = GetPatternType(score)
        };

        var results = new List<TestResult>
        {
            new()
            {
                TestName        = "Stillness",
                NormalizedScore = score.Stability,
                PrimaryMetric   = SessionData.TremorRms,
                TremorRms       = SessionData.TremorRms
            },
            new()
            {
                TestName        = "Strike",
                NormalizedScore = score.Reaction,
                PrimaryMetric   = SessionData.MedianRt,
                MedianRt        = SessionData.MedianRt,
                RtStdDev        = SessionData.RtStdDev,
                LapseCount      = SessionData.LapseCount
            },
            new()
            {
                TestName        = "Aim",
                NormalizedScore = score.Precision,
                PrimaryMetric   = SessionData.CentroidError < 990
                                    ? SessionData.CentroidError : 1.00,
                CentroidError   = SessionData.CentroidError < 990
                                    ? SessionData.CentroidError : null
            },
            new()
            {
                TestName        = "Chase",
                NormalizedScore = score.DualTask,
                PrimaryMetric   = SessionData.TrackingError < 990
                                    ? SessionData.TrackingError / 180.0 : 1.00,
                TrackingError   = SessionData.TrackingError < 990
                                    ? SessionData.TrackingError : null,
                StabilityRms    = SessionData.StabilityRms
            },
            new()
            {
                TestName        = "Pulse",
                NormalizedScore = score.Cardiac,
                PrimaryMetric   = SessionData.HrvRmssd,
                HeartRate       = SessionData.HeartRate,
                HrvRmssd        = SessionData.HrvRmssd
            }
        };

        return (session, results);
    }
}

/// <summary>Per-dimension and composite scores for one completed session.</summary>
public class SessionScore
{
    public int Stability { get; set; }
    public int Reaction  { get; set; }
    public int Precision { get; set; }
    public int DualTask  { get; set; }
    public int Cardiac   { get; set; }
    public int Composite { get; set; }
}
