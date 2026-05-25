using System.Numerics;
using Reflex.Graphics;
using Reflex.Services;

namespace Reflex.Pages;

public partial class StillnessPage : ContentPage
{
    private CancellationTokenSource _cts = new();
    private readonly WaveformDrawable _drawable = new(300);

    // Latest raw readings (updated by sensor callbacks)
    private Vector3 _latestAccel;
    private Vector3 _latestGyro;

    // Rolling window for DC removal (gravity subtraction)
    private readonly Queue<float> _accelMagWindow = new(100);

    // All deviation samples collected this session
    private readonly List<float> _samples = new();

    public StillnessPage()
    {
        InitializeComponent();
        WaveformView.Drawable = _drawable;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _cts = new CancellationTokenSource();
        _samples.Clear();
        _accelMagWindow.Clear();
        StartSensors();
        _ = CountdownAsync(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts.Cancel();
        StopSensors();
    }

    // ── Sensors ────────────────────────────────────────────────────────────

    private void StartSensors()
    {
        try
        {
            if (!Accelerometer.Default.IsMonitoring)
            {
                Accelerometer.Default.ReadingChanged += OnAccelChanged;
                Accelerometer.Default.Start(SensorSpeed.Fastest);
            }
        }
        catch (FeatureNotSupportedException) { /* Windows / simulator */ }

        try
        {
            if (!Gyroscope.Default.IsMonitoring)
            {
                Gyroscope.Default.ReadingChanged += OnGyroChanged;
                Gyroscope.Default.Start(SensorSpeed.Fastest);
            }
        }
        catch (FeatureNotSupportedException) { /* Windows / simulator */ }
    }

    private void StopSensors()
    {
        try
        {
            if (Accelerometer.Default.IsMonitoring)
            {
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= OnAccelChanged;
            }
        }
        catch { }

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

    private void OnAccelChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        _latestAccel = e.Reading.Acceleration;
        ProcessSample();
    }

    private void OnGyroChanged(object? sender, GyroscopeChangedEventArgs e)
        => _latestGyro = e.Reading.AngularVelocity;

    // ── Signal processing ──────────────────────────────────────────────────

    private void ProcessSample()
    {
        // Magnitude of acceleration vector (includes ~1g gravity at rest)
        float mag = _latestAccel.Length();

        // Rolling window — maintain last 100 readings for DC estimate
        if (_accelMagWindow.Count >= 100) _accelMagWindow.Dequeue();
        _accelMagWindow.Enqueue(mag);

        // Tremor = deviation from the local mean (removes gravity)
        float mean      = _accelMagWindow.Average();
        float deviation = MathF.Abs(mag - mean);

        // Add scaled gyro contribution (rad/s → approximate g equivalent)
        float gyroContrib = _latestGyro.Length() * 0.02f;

        float sample = deviation + gyroContrib;
        _samples.Add(sample);
        _drawable.AddSample(sample);

        MainThread.BeginInvokeOnMainThread(() => WaveformView.Invalidate());
    }

    // ── Countdown ──────────────────────────────────────────────────────────

    private async Task CountdownAsync(CancellationToken token)
    {
        for (int i = 30; i >= 0; i--)
        {
            if (token.IsCancellationRequested) return;
            MainThread.BeginInvokeOnMainThread(() => TimerLabel.Text = i.ToString());
            try { await Task.Delay(1000, token); }
            catch (TaskCanceledException) { return; }
        }

        if (!token.IsCancellationRequested)
        {
            SaveResult();
            await Shell.Current.GoToAsync(nameof(StrikePage));
        }
    }

    // ── Result ─────────────────────────────────────────────────────────────

    private void SaveResult()
    {
        if (_samples.Count == 0)
        {
            SessionData.TremorRms = 0;
            return;
        }

        // RMS of all deviation samples
        double sumSq = _samples.Sum(x => (double)x * x);
        SessionData.TremorRms = Math.Sqrt(sumSq / _samples.Count);
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private async void OnNextClicked(object sender, EventArgs e)
    {
        _cts.Cancel();
        SaveResult();
        await Shell.Current.GoToAsync(nameof(StrikePage));
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _cts.Cancel();
        SessionData.Reset();
        await Shell.Current.Navigation.PopToRootAsync();
    }
}
