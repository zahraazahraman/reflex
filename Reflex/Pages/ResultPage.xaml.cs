namespace Reflex.Pages;

public partial class ResultPage : ContentPage
{
    public ResultPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Placeholder values — replaced by real scoring in Phase 3/4
        ScoreLabel.Text      = "72";
        BandLabel.Text       = "Good";
        DiagnosisLabel.Text  = "All systems within normal range.";

        StabilityBar.Progress = 0.80; StabilityScore.Text = "80";
        ReactionBar.Progress  = 0.75; ReactionScore.Text  = "75";
        PrecisionBar.Progress = 0.70; PrecisionScore.Text = "70";
        DualTaskBar.Progress  = 0.68; DualTaskScore.Text  = "68";
        CardiacBar.Progress   = 0.60; CardiacScore.Text   = "60";
    }

    private async void OnTestAgainClicked(object sender, EventArgs e)
        => await Shell.Current.Navigation.PopToRootAsync();

    private async void OnHistoryClicked(object sender, EventArgs e)
        => await DisplayAlert("Coming soon", "History view will be added in the next phase.", "OK");
}
