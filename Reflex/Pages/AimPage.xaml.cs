namespace Reflex.Pages;

public partial class AimPage : ContentPage
{
    public AimPage()
    {
        InitializeComponent();
    }

    private async void OnNextClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ChasePage));

    private async void OnCancelClicked(object sender, EventArgs e)
        => await Shell.Current.Navigation.PopToRootAsync();
}
