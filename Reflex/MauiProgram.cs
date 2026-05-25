using Microsoft.Extensions.Logging;
using Reflex.Data;
using Reflex.Pages;

namespace Reflex;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Data
        builder.Services.AddSingleton<DatabaseService>();

        // Pages
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<BriefingPage>();
        builder.Services.AddTransient<StillnessPage>();
        builder.Services.AddTransient<StrikePage>();
        builder.Services.AddTransient<AimPage>();
        builder.Services.AddTransient<ChasePage>();
        builder.Services.AddTransient<PulsePage>();
        builder.Services.AddTransient<ProcessingPage>();
        builder.Services.AddTransient<ResultPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
