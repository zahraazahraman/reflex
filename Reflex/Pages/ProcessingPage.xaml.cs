namespace Reflex.Pages;

public partial class ProcessingPage : ContentPage
{
    public ProcessingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(2000);
        await Shell.Current.GoToAsync(nameof(ResultPage));
    }
}
