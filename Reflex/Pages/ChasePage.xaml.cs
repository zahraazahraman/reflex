namespace Reflex.Pages;

public partial class ChasePage : ContentPage
{
    public ChasePage()
    {
        InitializeComponent();
    }

    private async void OnNextClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(PulsePage));

    private async void OnCancelClicked(object sender, EventArgs e)
        => await Shell.Current.Navigation.PopToRootAsync();
}
