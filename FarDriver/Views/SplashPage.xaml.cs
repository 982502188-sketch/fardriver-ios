namespace FarDriver.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
        Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(2), async () =>
        {
            await Navigation.PushAsync(new MainPage());
            Navigation.RemovePage(this);
        });
    }
}
