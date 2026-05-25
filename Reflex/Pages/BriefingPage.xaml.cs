namespace Reflex.Pages;

public partial class BriefingPage : ContentPage
{
    private CancellationTokenSource _cts = new();

    public BriefingPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _cts = new CancellationTokenSource();
        _ = AutoAdvanceAsync(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts.Cancel();
    }

    private async Task AutoAdvanceAsync(CancellationToken token)
    {
        for (int i = 10; i > 0; i--)
        {
            if (token.IsCancellationRequested) return;
            CountdownLabel.Text = $"Starting in {i}…";
            await Task.Delay(1000).ContinueWith(_ => { });
        }
        if (!token.IsCancellationRequested)
            await Shell.Current.GoToAsync(nameof(StillnessPage));
    }

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        _cts.Cancel();
        await Shell.Current.GoToAsync(nameof(StillnessPage));
    }
}
