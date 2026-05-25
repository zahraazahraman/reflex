using SQLite;

namespace Reflex.Models;

public class Session
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public double CompositeScore { get; set; }

    // Peak / Good / Caution / Impaired / Critical
    public string ScoreBand { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    // Fatigue / Stress / null
    public string? PatternType { get; set; }
}
