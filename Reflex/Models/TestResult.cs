using SQLite;

namespace Reflex.Models;

public class TestResult
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessionId { get; set; }

    // Stillness / Strike / Aim / Chase / Pulse
    public string TestName { get; set; } = string.Empty;

    // 0–100, feeds into the composite score
    public double NormalizedScore { get; set; }

    // The single value fed into the scoring formula (used for baseline comparison)
    public double PrimaryMetric { get; set; }

    // --- Stillness ---
    public double? TremorRms { get; set; }       // g, 6-axis RMS

    // --- Strike ---
    public double? MedianRt { get; set; }         // ms
    public double? RtStdDev { get; set; }         // ms
    public int? LapseCount { get; set; }          // trials > 500ms

    // --- Aim ---
    public double? CentroidError { get; set; }    // normalized (distance / radius)

    // --- Chase ---
    public double? TrackingError { get; set; }    // mean px distance from dot
    public double? StabilityRms { get; set; }     // gyro RMS during tracking

    // --- Pulse ---
    public double? HeartRate { get; set; }        // BPM
    public double? HrvRmssd { get; set; }         // ms
}
