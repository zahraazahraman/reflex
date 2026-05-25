using System.Diagnostics;
using System.Numerics;
using Reflex.Graphics;
using Reflex.Services;

namespace Reflex.Pages;

public partial class ChasePage : ContentPage
{
    private const double TestDurationSec = 30.0;
    private const double GlowThreshold   = 0.35;  // rad/s gyro magnitude → edge glow

    // Lissajous frequencies (irrational ratio → path never repeats in 30 s)
    private const double FreqX = 0.618;   // Hz  (golden ratio)
    private const double FreqY = 1.000;   // Hz

    private readonly ChaseDrawable _drawable = new();
    private IDispatcherTimer?      _timer;
    private readonly Stopwatch     _stopwatch = new();

    // Current dot position (canvas coordinates)
    private float _dotX;
    private float _dotY;

    // Finger tracking via PanGestureRecognizer
    private bool  _isTouching;
    private Point _touchStart;
    private Point _touchCurrent;

    // Gyroscope
    private float _gyroMag;

    // Collected samples
    private readonly List<double> _trackingErrors = new();
    private readonly List<float>  _gyroSamples    = new();

    private bool _finished;

    public ChasePage()
    {
        InitializeComponent();
        ChaseView.Drawable = _drawable;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _finished = false;
        _trackingErrors.Clear();
        _gyroSamples.Clear();
        _isTouching = false;

        StartGyroscope();

        _stopwatch.Restart();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);   // ~60 fps
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _stopwatch.Stop();
        StopGyroscope();
    }

    // ── Game loop ──────────────────────────────────────────────────────────

    private void OnTick(object? sender, EventArgs e)
    {
        double t = _stopwatch.Elapsed.TotalSeconds;
        if (_finished) return;

        double w = ChaseView.Width  > 0 ? ChaseView.Width  : 360;
        double h = ChaseView.Height > 0 ? ChaseView.Height : 500;

        // Lissajous position
        _dotX = (float)(w / 2 + w * 0.38 * Math.Sin(2 * Math.PI * FreqX * t));
        _dotY = (float)(h / 2 + h * 0.38 * Math.Sin(2 * Math.PI * FreqY * t));

        // Update drawable
        _drawable.DotX      = _dotX;
        _drawable.DotY      = _dotY;
        _drawable.IsGlowing = _gyroMag > GlowThreshold;
        ChaseView.Invalidate();

        // Sample tracking error when finger is on screen
        if (_isTouching)
        {
            double dx   = _touchCurrent.X - _dotX;
            double dy   = _touchCurrent.Y - _dotY;
            _trackingErrors.Add(Math.Sqrt(dx * dx + dy * dy));
        }

        // Sample gyro
        _gyroSamples.Add(_gyroMag);

        // Countdown label
        int remaining = Math.Max(0, (int)(TestDurationSec - t));
        TimerLabel.Text = remaining.ToString();

        // Finish
        if (t >= TestDurationSec)
        {
            _finished = true;
            _timer?.Stop();
            SaveResult();
            _ = Shell.Current.GoToAsync(nameof(PulsePage));
        }
    }

    // ── Pan gesture — finger tracking ─────────────────────────────────────

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // Assume user placed finger on (or near) the dot
                _touchStart   = new Point(_dotX, _dotY);
                _touchCurrent = _touchStart;
                _isTouching   = true;
                break;

            case GestureStatus.Running:
                _touchCurrent = new Point(
                    _touchStart.X + e.TotalX,
                    _touchStart.Y + e.TotalY);
                _isTouching = true;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isTouching = false;
                break;
        }
    }

    // ── Gyroscope ──────────────────────────────────────────────────────────

    private void StartGyroscope()
    {
        try
        {
            if (!Gyroscope.Default.IsMonitoring)
            {
                Gyroscope.Default.ReadingChanged += OnGyroChanged;
                Gyroscope.Default.Start(SensorSpeed.Fastest);
            }
        }
        catch (FeatureNotSupportedException) { /* Windows */ }
    }

    private void StopGyroscope()
    {
        try
        {
            if (Gyroscope.Default.IsMonitoring)
            {
                Gyroscope.Default.Stop();
                Gyroscope.Default.ReadingChanged -= OnGyroChanged;
            }
        }
        catch { }
    }

    private void OnGyroChanged(object? sender, GyroscopeChangedEventArgs e)
    {
        var v = e.Reading.AngularVelocity;
        _gyroMag = v.Length();
    }

    // ── Result ─────────────────────────────────────────────────────────────

    private void SaveResult()
    {
        // Mean pixel tracking error (0 if user never touched)
        SessionData.TrackingError = _trackingErrors.Count > 0
            ? _trackingErrors.Average()
            : 999;

        // Gyroscope RMS over the 30-second window
        if (_gyroSamples.Count > 0)
        {
            double sumSq = _gyroSamples.Sum(x => (double)x * x);
            SessionData.StabilityRms = Math.Sqrt(sumSq / _gyroSamples.Count);
        }
        else
        {
            SessionData.StabilityRms = 0;
        }
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        _finished = true;
        _timer?.Stop();
        await Shell.Current.GoToAsync(nameof(PulsePage));
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _finished = true;
        _timer?.Stop();
        SessionData.Reset();
        await Shell.Current.Navigation.PopToRootAsync();
    }
}
