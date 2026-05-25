using Reflex.Services;

namespace Reflex.Pages;

public partial class ResultPage : ContentPage
{
    public ResultPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ── Compute real scores from session data ──────────────────────
        var score = ScoringService.Compute();
        var color = ScoringService.GetBandColor(score.Composite);

        // ── Composite header ───────────────────────────────────────────
        ScoreLabel.Text      = score.Composite.ToString();
        ScoreLabel.TextColor = color;
        BandLabel.Text       = ScoringService.GetBand(score.Composite);
        BandLabel.TextColor  = color;
        DiagnosisLabel.Text  = ScoringService.GetDiagnosis(score.Composite);

        // ── Dimension score labels ─────────────────────────────────────
        StabilityScore.Text = score.Stability.ToString();
        ReactionScore.Text  = score.Reaction.ToString();
        PrecisionScore.Text = score.Precision.ToString();
        DualTaskScore.Text  = score.DualTask.ToString();
        CardiacScore.Text   = score.Cardiac.ToString();

        // ── Animate bars from 0 → target ──────────────────────────────
        await AnimateBarsAsync(score);

        // ── Persist to database (non-blocking) ────────────────────────
        _ = SaveSessionAsync(score);
    }

    // ── Bar animation ──────────────────────────────────────────────────────

    private Task AnimateBarsAsync(SessionScore score)
    {
        const uint duration = 700;
        return Task.WhenAll(
            StabilityBar.ProgressTo(score.Stability / 100.0, duration, Easing.CubicOut),
            ReactionBar .ProgressTo(score.Reaction  / 100.0, duration, Easing.CubicOut),
            PrecisionBar.ProgressTo(score.Precision / 100.0, duration, Easing.CubicOut),
            DualTaskBar .ProgressTo(score.DualTask  / 100.0, duration, Easing.CubicOut),
            CardiacBar  .ProgressTo(score.Cardiac   / 100.0, duration, Easing.CubicOut)
        );
    }

    // ── Database save ──────────────────────────────────────────────────────

    private static async Task SaveSessionAsync(SessionScore score)
    {
        try
        {
            var (session, results) = ScoringService.BuildDbObjects(score);
            await App.Database.SaveSessionAsync(session, results);
        }
        catch
        {
            // Don't crash the result screen if the DB write fails
        }
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private async void OnTestAgainClicked(object sender, EventArgs e)
        => await Shell.Current.Navigation.PopToRootAsync();

    private async void OnHistoryClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(HistoryPage));
}
