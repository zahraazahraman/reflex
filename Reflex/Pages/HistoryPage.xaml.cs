using Reflex.Services;

namespace Reflex.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SessionRows.Children.Clear();

        var sessions = await App.Database.GetRecentSessionsAsync(7);

        if (sessions.Count == 0)
        {
            SessionRows.Children.Add(new Label
            {
                Text              = "No sessions yet.\nComplete your first test to see results here.",
                TextColor         = Color.FromArgb("#555566"),
                FontSize          = 14,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin            = new Thickness(0, 40, 0, 0)
            });
            return;
        }

        foreach (var session in sessions)
        {
            var scoreInt = (int)Math.Round(session.CompositeScore);
            var bandColor = ScoringService.GetBandColor(scoreInt);
            var dateStr   = session.CreatedAt.ToLocalTime().ToString("MMM d  ·  h:mm tt");

            // ── Outer card ───────────────────────────────────────────────
            var card = new Frame
            {
                BackgroundColor = Color.FromArgb("#12121F"),
                BorderColor     = Color.FromArgb("#1E1E35"),
                CornerRadius    = 12,
                Padding         = new Thickness(20, 16),
                Margin          = new Thickness(0, 0, 0, 12)
            };

            // ── 2-column grid: text left, score right ────────────────────
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // Date label (row 0, col 0)
            var dateLabel = new Label
            {
                Text      = dateStr,
                FontSize  = 12,
                TextColor = Color.FromArgb("#555566")
            };

            // Score (spans all rows, right column)
            var scoreLabel = new Label
            {
                Text              = scoreInt.ToString(),
                FontSize          = 40,
                FontAttributes    = FontAttributes.Bold,
                TextColor         = bandColor,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions   = LayoutOptions.Center,
                Margin            = new Thickness(16, 0, 0, 0)
            };

            // Band label (row 1, col 0)
            var bandLabel = new Label
            {
                Text           = session.ScoreBand,
                FontSize       = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor      = Color.FromArgb("#CCCCDD"),
                Margin         = new Thickness(0, 4, 0, 0)
            };

            // Diagnosis (row 2, col 0)
            var diagLabel = new Label
            {
                Text      = session.Diagnosis,
                FontSize  = 12,
                TextColor = Color.FromArgb("#555566"),
                Margin    = new Thickness(0, 4, 0, 0)
            };

            grid.Add(dateLabel,  column: 0, row: 0);
            grid.Add(scoreLabel, column: 1, row: 0);
            Grid.SetRowSpan(scoreLabel, 3);
            grid.Add(bandLabel,  column: 0, row: 1);
            grid.Add(diagLabel,  column: 0, row: 2);

            card.Content = grid;
            SessionRows.Children.Add(card);
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
