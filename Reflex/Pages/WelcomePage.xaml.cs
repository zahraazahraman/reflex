using Reflex.Data;
using Reflex.Services;

namespace Reflex.Pages;

public partial class WelcomePage : ContentPage
{
    private readonly DatabaseService _db;

    public WelcomePage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var last = await _db.GetLastSessionAsync();
        if (last is not null)
        {
            LastScoreLabel.Text    = $"Last score: {last.CompositeScore:F0}  ·  {last.ScoreBand}";
            LastTimeLabel.Text     = last.CreatedAt.ToLocalTime().ToString("MMM d, h:mm tt");
            LastSessionFrame.IsVisible = true;
        }
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        SessionData.Reset();   // clear any previous session data
        await Shell.Current.GoToAsync(nameof(BriefingPage));
    }
}
