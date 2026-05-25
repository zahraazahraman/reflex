using Reflex.Services;

namespace Reflex.Pages;

public partial class AimPage : ContentPage
{
    // 20 circles: 5 each at 80 / 60 / 40 / 20 px diameter
    private static readonly int[] Sizes =
        Enumerable.Repeat(80, 5)
            .Concat(Enumerable.Repeat(60, 5))
            .Concat(Enumerable.Repeat(40, 5))
            .Concat(Enumerable.Repeat(20, 5))
            .ToArray();

    private const int   MaxPerTargetMs = 3000;
    private const double TimeoutError  = 1.5;  // normalised error for a missed target

    private readonly Random _rng = new();
    private CancellationTokenSource _cts = new();

    // Current circle geometry (area-relative coordinates)
    private double _circleCx;   // centre X in TargetArea
    private double _circleCy;   // centre Y in TargetArea
    private double _circleR;    // radius

    // Signals a tap: value = normalised centroid error
    private TaskCompletionSource<double>? _tapTcs;

    public AimPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _cts = new CancellationTokenSource();
        _ = RunTargetsAsync(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts.Cancel();
        _tapTcs?.TrySetCanceled();
    }

    // ── Target loop ────────────────────────────────────────────────────────

    private async Task RunTargetsAsync(CancellationToken token)
    {
        var errors = new List<double>();

        for (int i = 0; i < Sizes.Length; i++)
        {
            if (token.IsCancellationRequested) return;

            int size   = Sizes[i];
            double r   = size / 2.0;
            int    num = i + 1;

            // Update header label
            MainThread.BeginInvokeOnMainThread(
                () => TargetLabel.Text = $"Target {num} / {Sizes.Length}  ·  {size}px");

            // Random position — keep circle fully inside the area
            double areaW = TargetArea.Width  > size ? TargetArea.Width  : 360;
            double areaH = TargetArea.Height > size ? TargetArea.Height : 500;
            double x = _rng.NextDouble() * (areaW - size);
            double y = _rng.NextDouble() * (areaH - size);

            // Store centre for error calculation
            _circleCx = x + r;
            _circleCy = y + r;
            _circleR  = r;

            // Show circle
            _tapTcs = new TaskCompletionSource<double>();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AbsoluteLayout.SetLayoutBounds(AimCircle, new Rect(x, y, size, size));
                AbsoluteLayout.SetLayoutFlags(AimCircle,
                    Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
                AimCircle.CornerRadius = r;
                AimCircle.IsVisible = true;
                TimeBar.Progress = 1;
            });

            // Animate the countdown bar while waiting for tap
            var barCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = AnimateBarAsync(barCts.Token);

            // Wait for tap or timeout
            var timeoutTask = Task.Delay(MaxPerTargetMs, token);
            var tapTask     = _tapTcs.Task;
            var winner      = await Task.WhenAny(tapTask, timeoutTask);

            barCts.Cancel();

            // Hide circle
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AimCircle.IsVisible = false;
                TimeBar.Progress    = 0;
            });

            double err = (winner == tapTask && tapTask.IsCompletedSuccessfully)
                ? tapTask.Result
                : TimeoutError;

            if (token.IsCancellationRequested) return;
            errors.Add(err);

            // Brief gap between targets
            try { await Task.Delay(200, token); }
            catch (TaskCanceledException) { return; }
        }

        SaveResult(errors);
        await Shell.Current.GoToAsync(nameof(ChasePage));
    }

    // ── Countdown bar animation ────────────────────────────────────────────

    private async Task AnimateBarAsync(CancellationToken token)
    {
        const int Steps    = 60;
        int       delayMs  = MaxPerTargetMs / Steps;

        for (int s = Steps; s >= 0; s--)
        {
            if (token.IsCancellationRequested) return;
            double progress = s / (double)Steps;
            MainThread.BeginInvokeOnMainThread(() => TimeBar.Progress = (float)progress);
            try { await Task.Delay(delayMs, token); }
            catch (TaskCanceledException) { return; }
        }
    }

    // ── Tap handler ────────────────────────────────────────────────────────

    private void OnAreaTapped(object? sender, TappedEventArgs e)
    {
        if (_tapTcs is null || _tapTcs.Task.IsCompleted) return;

        var pos = e.GetPosition(TargetArea);
        if (pos is null) return;

        double dx   = pos.Value.X - _circleCx;
        double dy   = pos.Value.Y - _circleCy;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // Normalise: 0 = perfect centre, 1 = edge of circle, >1 = outside
        double normError = dist / _circleR;

        _tapTcs.TrySetResult(normError);
    }

    // ── Result ─────────────────────────────────────────────────────────────

    private static void SaveResult(List<double> errors)
    {
        if (errors.Count == 0) return;
        SessionData.CentroidError = errors.Average();
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        _cts.Cancel();
        await Shell.Current.GoToAsync(nameof(ChasePage));
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _cts.Cancel();
        SessionData.Reset();
        await Shell.Current.Navigation.PopToRootAsync();
    }
}
