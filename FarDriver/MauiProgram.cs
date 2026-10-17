using Microsoft.Maui.LifecycleEvents;

namespace FarDriver;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if IOS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddiOS(ios => ios.FinishedLaunching((app, options) =>
            {
                return true;
            }));
        });
#endif

        builder.Services.AddSingleton<Services.IBleService, Services.BleService>();
        builder.Services.AddTransient<ViewModels.MainViewModel>();
        builder.Services.AddTransient<Views.MainPage>();

        return builder.Build();
    }
}
