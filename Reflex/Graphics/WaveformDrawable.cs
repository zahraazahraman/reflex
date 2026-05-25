namespace Reflex.Graphics;

/// <summary>
/// Scrolling waveform rendered on a GraphicsView.
/// Feed samples via AddSample(); call GraphicsView.Invalidate() to repaint.
/// </summary>
public class WaveformDrawable : IDrawable
{
    private readonly float[] _samples;
    private int  _head;       // index of the oldest sample (write position)
    private int  _count;      // how many samples are filled

    // Visual scale: values at this g level reach the top/bottom edge
    private const float ScaleG = 0.12f;

    public WaveformDrawable(int capacity = 300)
    {
        _samples = new float[capacity];
    }

    public void AddSample(float value)
    {
        _samples[_head] = value;
        _head = (_head + 1) % _samples.Length;
        if (_count < _samples.Length) _count++;
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        // Background
        canvas.FillColor = Color.FromArgb("#0D0D1A");
        canvas.FillRectangle(rect);

        // Centre line (zero-level guide)
        canvas.StrokeColor = Color.FromArgb("#1A2A4A");
        canvas.StrokeSize  = 1;
        canvas.DrawLine(rect.Left, rect.Center.Y, rect.Right, rect.Center.Y);

        if (_count < 2) return;

        // Build ordered sample array (oldest → newest)
        int len  = _count;
        float step = rect.Width / len;

        canvas.StrokeColor = Color.FromArgb("#00E5FF");
        canvas.StrokeSize  = 2;

        var path = new PathF();
        bool started = false;

        for (int i = 0; i < len; i++)
        {
            int idx = (_head - len + i + _samples.Length) % _samples.Length;
            float v = _samples[idx];

            // Map deviation to y: 0 → midY, +ScaleG → top, larger → clamped
            float norm = Math.Clamp(v / ScaleG, -1f, 1f);
            float x = rect.Left + i * step;
            float y = rect.Center.Y - norm * (rect.Height * 0.45f);

            if (!started) { path.MoveTo(x, y); started = true; }
            else          { path.LineTo(x, y); }
        }

        canvas.DrawPath(path);
    }
}
