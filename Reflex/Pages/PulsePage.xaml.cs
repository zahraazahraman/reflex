using System.Diagnostics;
using Reflex.Services;

namespace Reflex.Pages;

public partial class PulsePage : ContentPage
{
    private const int TestDurationSec  = 30;
    private const int SampleIntervalMs = 33;   // ~30 Hz

    private readonly RppgProcessor _processor = new();
    private IDispatcherTimer?       _timer;
    private readonly Stopwatch      _stopwatch = new();
    private bool _finished;

    // Beat animation gating — fires once per detected heartbeat
    private int _lastBeatCount = -1;

    public PulsePage()
    {
        InitializeComponent();
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _finished      = false;
        _lastBeatCount = -1;

        _stopwatch.Restart();
        _timer          = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(SampleIntervalMs);
        _timer.Tick    += OnTick;
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _stopwatch.Stop();
    }

    // ── Sampling loop ─────────────────────────────────────────────────────

    private async void OnTick(object? sender, EventArgs e)
    {
        if (_finished) return;

        double t = _stopwatch.Elapsed.TotalSeconds;

        // Feed the DSP pipeline with a simulated PPG sample.
        // On a real device this would come from the camera red channel.
        _processor.AddSimulatedSample();

        // Countdown label
        int remaining = Math.Max(0, (int)(TestDurationSec - t));
        TimerLabel.Text = remaining.ToString();

        // Update readout once BPM is available
        if (_processor.IsReady && _processor.Bpm > 0)
        {
            BpmLabel.Text  = $"{_processor.Bpm:F0} BPM";
            StatusLabel.Text = "Measuring ♥";

            if (_processor.HrvRmssd > 0)
                HrvLabel.Text = $"HRV: {_processor.HrvRmssd:F0} ms";

            // Pulse animation — one visual beat per detected cardiac cycle
            double beatPeriodSec = 60.0 / _processor.Bpm;
            int    beatCount     = (int)(t / beatPeriodSec);
            if (beatCount != _lastBeatCount)
            {
                _lastBeatCount = beatCount;
                _ = PulseAnimationAsync();
            }
        }

        // Finish
        if (t >= TestDurationSec)
        {
            _finished = true;
            _timer?.Stop();
            SaveResult();
            await Shell.Current.GoToAsync(nameof(ProcessingPage));
        }
    }

    // ── Heart-circle pulse animation ──────────────────────────────────────

    private async Task PulseAnimationAsync()
    {
        await HeartCircle.ScaleTo(1.16, 120, Easing.CubicOut);
        await HeartCircle.ScaleTo(1.00, 160, Easing.CubicIn);
    }

    // ── Result ─────────────────────────────────────────────────────────────

    private void SaveResult()
    {
        // Store 0 if the signal was never acquired — the scoring engine
        // treats 0 as "not measured" and substitutes a neutral 50 score.
        SessionData.HeartRate = _processor.Bpm      > 0 ? _processor.Bpm      : 0;
        SessionData.HrvRmssd  = _processor.HrvRmssd > 0 ? _processor.HrvRmssd : 0;
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        _finished = true;
        _timer?.Stop();
        SaveResult();
        await Shell.Current.GoToAsync(nameof(ProcessingPage));
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _finished = true;
        _timer?.Stop();
        SessionData.Reset();
        await Shell.Current.Navigation.PopToRootAsync();
    }
}
