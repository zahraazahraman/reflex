namespace Reflex.Services;

/// <summary>
/// Remote photoplethysmography (rPPG) signal processor.
///
/// Pipeline:
///   raw sample → DC removal (2-s moving avg) → 1st-order IIR low-pass
///   → ring buffer → peak detection → IBI → BPM + HRV RMSSD
///
/// On a real device feed real camera red-channel means via AddSample().
/// On Windows / emulator call AddSimulatedSample() at ~30 Hz instead.
/// </summary>
public class RppgProcessor
{
    // ── Constants ──────────────────────────────────────────────────────────
    private const int    SampleRate       = 30;               // Hz
    private const int    BufferSamples    = SampleRate * 32;  // 32-s rolling window
    private const int    DcWindowSize     = SampleRate * 2;   // 2-s DC removal
    private const int    WarmupSamples    = SampleRate * 8;   // 8 s before first BPM
    private const int    MinPeakDist      = (int)(SampleRate * 0.35); // 350 ms min IBI
    private const double LpAlpha          = 0.432;            // IIR LP ~4 Hz @ 30 Hz

    // ── Ring buffer ────────────────────────────────────────────────────────
    private readonly float[] _buf    = new float[BufferSamples];
    private int               _head  = 0;   // next write position
    private int               _count = 0;   // valid samples (≤ BufferSamples)
    private int               _total = 0;   // ever-incrementing, for update cadence

    // ── DC removal state ──────────────────────────────────────────────────
    private readonly Queue<float> _dcWin = new();
    private float                 _dcSum = 0;

    // ── IIR LP state ──────────────────────────────────────────────────────
    private double _lpPrev = 0;

    // ── Results ───────────────────────────────────────────────────────────
    public float Bpm      { get; private set; }
    public float HrvRmssd { get; private set; }
    public bool  IsReady  => _count >= WarmupSamples;

    // ── Simulation state ──────────────────────────────────────────────────
    private readonly Random _rng            = new();
    private double          _simBpm;
    private double          _simPhase       = 0.0;
    private double          _simBreathPhase = 0.0;

    public RppgProcessor()
    {
        _simBpm = 62 + _rng.NextDouble() * 18;  // 62–80 BPM, random per session
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Generate and push one synthetic heartbeat sample.
    /// Call at ~30 Hz on platforms without camera access.
    /// </summary>
    public void AddSimulatedSample()
    {
        // Slow BPM drift (±0.5 BPM/s max)
        _simBpm += (_rng.NextDouble() - 0.5) * 0.01;
        _simBpm  = Math.Clamp(_simBpm, 50, 100);

        double dt = 1.0 / SampleRate;
        _simPhase       += (_simBpm / 60.0) * dt * 2 * Math.PI;
        _simBreathPhase += (15.0  / 60.0) * dt * 2 * Math.PI;  // ~15 breaths/min

        // Synthetic PPG: sharp systolic peak + smaller diastolic bump
        double phase    = _simPhase % (2 * Math.PI);
        double systole  = phase < 0.8
            ? Math.Pow(Math.Sin(phase * Math.PI / 0.8), 2)
            : 0.0;
        double diastole = phase > 1.5 && phase < 2.5
            ? 0.35 * Math.Pow(Math.Sin((phase - 1.5) * Math.PI), 2)
            : 0.0;
        double breath   = 1.0 + 0.06 * Math.Sin(_simBreathPhase);
        double noise    = (_rng.NextDouble() - 0.5) * 0.05;

        float sample = (float)(128 + 30 * (systole + diastole) * breath + noise);
        ProcessSample(sample);
    }

    /// <summary>Push one real camera red-channel mean (0–255).</summary>
    public void AddSample(float redChannelMean) => ProcessSample(redChannelMean);

    // ── DSP pipeline ───────────────────────────────────────────────────────

    private void ProcessSample(float raw)
    {
        // 1. DC removal: subtract running 2-s moving average
        _dcWin.Enqueue(raw);
        _dcSum += raw;
        if (_dcWin.Count > DcWindowSize)
            _dcSum -= _dcWin.Dequeue();
        float dcFree = raw - (_dcSum / _dcWin.Count);

        // 2. First-order IIR low-pass (~4 Hz cutoff at 30 Hz)
        _lpPrev = LpAlpha * _lpPrev + (1.0 - LpAlpha) * dcFree;

        // 3. Store in ring buffer
        _buf[_head] = (float)_lpPrev;
        _head  = (_head + 1) % BufferSamples;
        _count = Math.Min(_count + 1, BufferSamples);
        _total++;

        // 4. Recompute peaks once per second
        if (IsReady && _total % SampleRate == 0)
            DetectPeaks();
    }

    private void DetectPeaks()
    {
        // Reconstruct ordered signal from ring buffer
        int n   = _count;
        var sig = new float[n];
        for (int i = 0; i < n; i++)
            sig[i] = _buf[(_head - n + i + BufferSamples) % BufferSamples];

        // Adaptive threshold at 55 % of signal range
        float max = float.MinValue, min = float.MaxValue;
        foreach (float s in sig)
        {
            if (s > max) max = s;
            if (s < min) min = s;
        }
        if (max - min < 0.01f) return; // flat signal — no data yet
        float thresh = min + (max - min) * 0.55f;

        // Peak detection with minimum inter-peak distance
        var peaks = new List<int>();
        for (int i = MinPeakDist; i < n - MinPeakDist; i++)
        {
            if (sig[i] > thresh && sig[i] > sig[i - 1] && sig[i] > sig[i + 1])
            {
                if (peaks.Count == 0 || i - peaks[^1] >= MinPeakDist)
                    peaks.Add(i);
            }
        }

        if (peaks.Count < 3) return; // need ≥ 2 IBIs

        // IBI → BPM
        var ibis = new List<float>(peaks.Count - 1);
        for (int i = 1; i < peaks.Count; i++)
            ibis.Add((peaks[i] - peaks[i - 1]) * 1000f / SampleRate); // ms

        float meanIbi = ibis.Average();
        float bpm     = 60_000f / meanIbi;

        if (bpm >= 40f && bpm <= 180f)
            Bpm = MathF.Round(bpm);

        // HRV RMSSD
        if (ibis.Count >= 2)
        {
            double sumSq = 0;
            for (int i = 1; i < ibis.Count; i++)
            {
                double d = ibis[i] - ibis[i - 1];
                sumSq += d * d;
            }
            HrvRmssd = (float)Math.Round(Math.Sqrt(sumSq / (ibis.Count - 1)));
        }
    }
}
