namespace Reflex.Pages;

public partial class StrikePage : ContentPage
{
    public StrikePage()
    {
        InitializeComponent();
    }

    private async void OnNextClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(AimPage));

    private async void OnCancelClicked(object sender, EventArgs e)
        => await Shell.Current.Navigation.PopToRootAsync();
}
