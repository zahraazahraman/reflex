namespace Reflex.Services;

/// <summary>
/// Holds raw sensor metrics collected during the current test session.
/// Written by each test page, read by the scoring engine on the Result page.
/// </summary>
public static class SessionData
{
    // Test 01 — Stillness
    public static double TremorRms     { get; set; }   // g  (6-axis RMS deviation)

    // Test 02 — Strike
    public static double MedianRt      { get; set; }   // ms
    public static double RtStdDev      { get; set; }   // ms
    public static int    LapseCount    { get; set; }   // trials > 500 ms

    // Test 03 — Aim
    public static double CentroidError { get; set; }   // normalised (distance / radius)

    // Test 04 — Chase
    public static double TrackingError { get; set; }   // px mean distance
    public static double StabilityRms  { get; set; }   // gyro RMS during tracking

    // Test 05 — Pulse
    public static double HeartRate     { get; set; }   // BPM
    public static double HrvRmssd      { get; set; }   // ms

    /// <summary>Call this when a new session starts (Welcome → Briefing).</summary>
    public static void Reset()
    {
        TremorRms     = 0;
        MedianRt      = 0;
        RtStdDev      = 0;
        LapseCount    = 0;
        CentroidError = 0;
        TrackingError = 0;
        StabilityRms  = 0;
        HeartRate     = 0;
        HrvRmssd      = 0;
    }
}
