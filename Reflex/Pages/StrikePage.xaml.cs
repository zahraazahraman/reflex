using System.Diagnostics;
using Reflex.Services;

namespace Reflex.Pages;

public partial class StrikePage : ContentPage
{
    private const int TotalTrials   = 12;
    private const int MaxWaitMs     = 3000;  // circle disappears if not tapped within 3 s
    private const int LapseThreshMs = 500;   // RT > 500 ms counts as a lapse

    private readonly Random _rng = new();
    private CancellationTokenSource _cts = new();

    // Signals a tap: value = elapsed ms since circle appeared
    private TaskCompletionSource<long>? _tapTcs;
    private long _stimulusTick;   // Stopwatch.GetTimestamp() when circle was shown

    public StrikePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _cts = new CancellationTokenSource();
        _ = RunTrialsAsync(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts.Cancel();
        _tapTcs?.TrySetCanceled();
    }

    // ── Trial loop ─────────────────────────────────────────────────────────

    private async Task RunTrialsAsync(CancellationToken token)
    {
        var reactionTimes = new List<double>();

        for (int trial = 1; trial <= TotalTrials; trial++)
        {
            if (token.IsCancellationRequested) return;

            // Update counter
            int t = trial;
            MainThread.BeginInvokeOnMainThread(
                () => TrialLabel.Text = $"Trial {t} / {TotalTrials}");

            // ISI — random dark pause (prevents anticipation)
            int isi = _rng.Next(1000, 4001);
            try   { await Task.Delay(isi, token); }
            catch (TaskCanceledException) { return; }

            if (token.IsCancellationRequested) return;

            // Random position within the TargetArea
            double areaW = TargetArea.Width  > 80 ? TargetArea.Width  : 320;
            double areaH = TargetArea.Height > 80 ? TargetArea.Height : 480;
            double x = _rng.NextDouble() * (areaW - 80);
            double y = _rng.NextDouble() * (areaH - 80);

            // Show circle
            _tapTcs = new TaskCompletionSource<long>();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AbsoluteLayout.SetLayoutBounds(StrikeTarget, new Rect(x, y, 80, 80));
                AbsoluteLayout.SetLayoutFlags(StrikeTarget, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
                StrikeTarget.IsVisible = true;
                _stimulusTick = Stopwatch.GetTimestamp();
            });

            // Wait for tap or timeout
            Task timeoutTask = Task.Delay(MaxWaitMs, token);
            Task<long> tapTask = _tapTcs.Task;
            Task winner = await Task.WhenAny(tapTask, timeoutTask);

            // Hide circle immediately
            MainThread.BeginInvokeOnMainThread(() => StrikeTarget.IsVisible = false);

            double rtMs = (winner == tapTask && tapTask.IsCompletedSuccessfully)
                ? tapTask.Result
                : MaxWaitMs;   // timeout = worst-case lapse

            if (token.IsCancellationRequested) return;
            reactionTimes.Add(rtMs);
        }

        // All 12 trials done
        SaveResult(reactionTimes);
        await Shell.Current.GoToAsync(nameof(AimPage));
    }

    // ── Tap handler ────────────────────────────────────────────────────────

    private void OnTargetTapped(object? sender, TappedEventArgs e)
    {
        long now     = Stopwatch.GetTimestamp();
        long elapsed = (long)((now - _stimulusTick) * 1000.0 / Stopwatch.Frequency);
        _tapTcs?.TrySetResult(elapsed);
    }

    // ── Result ─────────────────────────────────────────────────────────────

    private static void SaveResult(List<double> rts)
    {
        if (rts.Count == 0) return;

        var sorted = rts.OrderBy(x => x).ToList();
        int mid    = sorted.Count / 2;

        double median = sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];

        double mean   = rts.Average();
        double stdDev = Math.Sqrt(rts.Sum(x => (x - mean) * (x - mean)) / rts.Count);
        int    lapses = rts.Count(x => x > LapseThreshMs);

        SessionData.MedianRt   = median;
        SessionData.RtStdDev   = stdDev;
        SessionData.LapseCount = lapses;
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        _cts.Cancel();
        await Shell.Current.GoToAsync(nameof(AimPage));
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _cts.Cancel();
        SessionData.Reset();
        await Shell.Current.Navigation.PopToRootAsync();
    }
}
