namespace Reflex.Pages;

public partial class PulsePage : ContentPage
{
    public PulsePage()
    {
        InitializeComponent();
    }

    private async void OnNextClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ProcessingPage));

    private async void OnCancelClicked(object sender, EventArgs e)
        => await Shell.Current.Navigation.PopToRootAsync();
}
