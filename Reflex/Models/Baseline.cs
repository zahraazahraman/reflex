using SQLite;

namespace Reflex.Models;

public class Baseline
{
    [PrimaryKey]
    public string TestName { get; set; } = string.Empty;

    // Personal best raw PrimaryMetric across all sessions
    public double BestValue { get; set; }

    // Lower = smaller is better (Stillness/Strike/Aim/Chase)
    // Higher = larger is better (Pulse HRV)
    public string Direction { get; set; } = "Lower";

    public DateTime UpdatedAt { get; set; }

    // How many sessions have contributed; 0 means seeded but never updated
    public int SessionCount { get; set; }
}
