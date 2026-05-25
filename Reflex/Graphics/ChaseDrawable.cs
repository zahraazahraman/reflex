namespace Reflex.Graphics;

/// <summary>
/// Renders the Chase test canvas:
///   - Dark background
///   - Cyan Lissajous dot with soft outer glow
///   - Red edge glow pulsed when the phone is trembling
/// </summary>
public class ChaseDrawable : IDrawable
{
    public float DotX      { get; set; }
    public float DotY      { get; set; }
    public bool  IsGlowing { get; set; }   // true when gyro exceeds tremor threshold

    private const float DotRadius  = 14f;
    private const float GlowRadius = 28f;

    public void Draw(ICanvas canvas, RectF rect)
    {
        // Background
        canvas.FillColor = Color.FromArgb("#050508");
        canvas.FillRectangle(rect);

        // Edge glow — subtle red pulse when phone is shaking
        if (IsGlowing)
        {
            canvas.FillColor = Color.FromArgb("#44FF1100");
            canvas.FillRectangle(rect);
        }

        // Outer soft halo
        canvas.FillColor = Color.FromArgb("#2200E5FF");
        canvas.FillCircle(DotX, DotY, GlowRadius);

        // Inner dot
        canvas.FillColor = Color.FromArgb("#00E5FF");
        canvas.FillCircle(DotX, DotY, DotRadius);
    }
}
